using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using CarniceriaWhatsApp.Models;
using CarniceriaWhatsApp.Services;

namespace CarniceriaWhatsApp.Pages.Admin
{
    public class ProductosModel : PageModel
    {
        private readonly ISupabaseService _supabase;
        private readonly HttpClient _httpClient;
        
        private const string MASTER_KEY = "DEV_MASTER_KEY_2026_CARNICERIA_DV7x9Kp2";
        
        public ProductosModel(ISupabaseService supabase, HttpClient httpClient)
        {
            _supabase = supabase;
            _httpClient = httpClient;
        }
        
        public List<Producto> Productos { get; set; } = new List<Producto>();
        public string Message { get; set; } = "";
        public bool IsError { get; set; }
        
        public bool MostrarBannerLicencia { get; set; } = false;
        public string MensajeBannerLicencia { get; set; } = "";
        public bool LicenciaActivadaEsteMes { get; set; } = false;
        
        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("AdminLogged") != "true")
                return RedirectToPage("/Admin/Login");
            
            try { Productos = await _supabase.ObtenerProductosAsync(); } catch {}
            await VerificarEstadoLicencia();
            
            if (TempData["LicenseWarning"] != null && !LicenciaActivadaEsteMes)
            {
                MostrarBannerLicencia = true;
                MensajeBannerLicencia = TempData["LicenseWarning"].ToString();
            }
            return Page();
        }
        
        public async Task<IActionResult> OnPostActivarLicenciaAsync(string MasterKey)
        {
            System.Console.WriteLine("========================================");
            System.Console.WriteLine($"[LICENCIA] 🚀 Intento de activación");
            
            if (string.IsNullOrEmpty(MasterKey) || MasterKey != MASTER_KEY)
            {
                Message = "❌ Clave incorrecta";
                IsError = true;
                await VerificarEstadoLicencia();
                try { Productos = await _supabase.ObtenerProductosAsync(); } catch {}
                return Page();
            }
            
            System.Console.WriteLine($"[LICENCIA] ✅ Clave maestra válida");
            
            try
            {
                var config = await _supabase.ObtenerConfiguracionAsync();
                var hoy = DateTime.Today;
                var mesActual = $"{hoy.Year}-{hoy.Month:D2}";
                
                System.Console.WriteLine($"[LICENCIA] 📋 Antes: pagada={config.LicenciaPagada}, hasta='{config.LicenciaPagadaHasta ?? "NULL"}'");
                
                // ✅ ACTUALIZAR DIRECTAMENTE VÍA HTTP PATCH (sin usar el servicio que tiene cache)
                var updateData = new
                {
                    licencia_pagada = true,
                    licencia_pagada_hasta = mesActual,
                    nota_licencia = $"Activado el {hoy:dd/MM/yyyy}",
                    actualizado_en = DateTime.UtcNow.ToString("o")
                };
                
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
                var json = JsonSerializer.Serialize(updateData, options);
                
                var supabaseUrl = Environment.GetEnvironmentVariable("Supabase__Url") 
                    ?? "https://wfmoyssyoyqnxqjqedcc.supabase.co";
                var supabaseKey = Environment.GetEnvironmentVariable("Supabase__ApiKey");
                
                if (string.IsNullOrEmpty(supabaseKey))
                {
                    // Fallback: usar el servicio si no hay API key
                    config.LicenciaPagada = true;
                    config.LicenciaPagadaHasta = mesActual;
                    config.NotaLicencia = $"Activado el {hoy:dd/MM/yyyy}";
                    await _supabase.ActualizarConfiguracionAsync(config);
                }
                else
                {
                    var configId = config.Id ?? 1;
                    var url = $"{supabaseUrl}/rest/v1/configuracion_carniceria?id=eq.{configId}";
                    
                    _httpClient.DefaultRequestHeaders.Clear();
                    _httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
                    _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
                    
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PatchAsync(url, content);
                    var responseBody = await response.Content.ReadAsStringAsync();
                    
                    System.Console.WriteLine($"[LICENCIA] 📡 Response: {response.StatusCode}");
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        System.Console.WriteLine($"[LICENCIA] ❌ Error: {responseBody}");
                    }
                }
                
                // ✅ GUARDAR EN SESIÓN QUE LA LICENCIA FUE ACTIVADA
                // Esto persiste entre requests y evita que el banner vuelva a aparecer
                HttpContext.Session.SetString("LicenciaActivada_" + mesActual, "true");
                
                Message = "✅ ¡Gracias! Licencia activada para este mes.";
                IsError = false;
                LicenciaActivadaEsteMes = true;
                MostrarBannerLicencia = false;
                
                System.Console.WriteLine($"[LICENCIA] ✅ Sesión guardada: LicenciaActivada_{mesActual}=true");
                System.Console.WriteLine("========================================");
                
                try { Productos = await _supabase.ObtenerProductosAsync(); } catch {}
                return Page(); // ✅ No redirigir, evitar re-lectura con cache
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[LICENCIA] ❌ Exception: {ex.Message}");
                Message = "❌ Error: " + ex.Message;
                IsError = true;
                await VerificarEstadoLicencia();
                try { Productos = await _supabase.ObtenerProductosAsync(); } catch {}
                return Page();
            }
        }
        
        private async Task VerificarEstadoLicencia()
        {
            try
            {
                var hoy = DateTime.Today;
                var diaActual = hoy.Day;
                var mesActual = $"{hoy.Year}-{hoy.Month:D2}";
                
                // ✅ 1. PRIMERO verificar si está en sesión (activación reciente)
                var licenciaEnSesion = HttpContext.Session.GetString("LicenciaActivada_" + mesActual);
                
                if (licenciaEnSesion == "true")
                {
                    System.Console.WriteLine($"[BANNER] 🔄 Usando sesión: licencia activada para {mesActual}");
                    MostrarBannerLicencia = false;
                    LicenciaActivadaEsteMes = true;
                    return; // ✅ Salir, no mostrar banner
                }
                
                // ✅ 2. Si no está en sesión, leer de BD con cache-busting
                var config = await ObtenerConfiguracionFrescaAsync();
                
                System.Console.WriteLine($"[BANNER] 🔍 BD: pagada={config.LicenciaPagada}, hasta='{config.LicenciaPagadaHasta ?? "NULL"}', mes='{mesActual}'");
                
                bool licenciaAlDia = config.LicenciaPagada && config.LicenciaPagadaHasta == mesActual;
                
                // ✅ 3. Si está al día en BD, guardar en sesión para próximos requests
                if (licenciaAlDia)
                {
                    HttpContext.Session.SetString("LicenciaActivada_" + mesActual, "true");
                    System.Console.WriteLine($"[BANNER] ✅ Sesión actualizada: LicenciaActivada_{mesActual}=true");
                }
                
                if (!licenciaAlDia && diaActual >= 1 && diaActual <= 10)
                {
                    var diasRestantes = 10 - diaActual;
                    MensajeBannerLicencia = $"⚠️ Recordatorio: Tu licencia vence el 10 de {hoy:MMMM}. Te quedan {diasRestantes} días para regularizar.";
                    MostrarBannerLicencia = true;
                    LicenciaActivadaEsteMes = false;
                    System.Console.WriteLine($"[BANNER] 🟡 Mostrando banner");
                }
                else if (licenciaAlDia)
                {
                    MostrarBannerLicencia = false;
                    LicenciaActivadaEsteMes = true;
                    System.Console.WriteLine($"[BANNER] 🟢 Licencia al día - SIN banner");
                }
                else
                {
                    MostrarBannerLicencia = false;
                    LicenciaActivadaEsteMes = false;
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[BANNER] ❌ Error: {ex.Message}");
            }
        }
        
        // ✅ LECTURA FRESCA CON CACHE-BUSTING
        private async Task<ConfiguracionCarniceria> ObtenerConfiguracionFrescaAsync()
        {
            try
            {
                var supabaseUrl = Environment.GetEnvironmentVariable("Supabase__Url") 
                    ?? "https://wfmoyssyoyqnxqjqedcc.supabase.co";
                var supabaseKey = Environment.GetEnvironmentVariable("Supabase__ApiKey");
                
                if (string.IsNullOrEmpty(supabaseKey))
                    return await _supabase.ObtenerConfiguracionAsync();
                
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var url = $"{supabaseUrl}/rest/v1/configuracion_carniceria?select=*&order=id.asc&limit=1&_t={timestamp}";
                
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", supabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
                
                var response = await client.GetAsync(url);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.GetArrayLength() > 0)
                    {
                        var element = doc.RootElement[0];
                        var config = new ConfiguracionCarniceria();
                        
                        if (element.TryGetProperty("id", out var id)) config.Id = id.GetInt64();
                        if (element.TryGetProperty("licencia_pagada", out var lp)) config.LicenciaPagada = lp.GetBoolean();
                        if (element.TryGetProperty("licencia_pagada_hasta", out var lh)) config.LicenciaPagadaHasta = lh.GetString();
                        if (element.TryGetProperty("nota_licencia", out var nl)) config.NotaLicencia = nl.GetString();
                        
                        System.Console.WriteLine($"[CONFIG] 🔄 Lectura fresca: pagada={config.LicenciaPagada}, hasta='{config.LicenciaPagadaHasta}'");
                        return config;
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[CONFIG] ❌ Error: {ex.Message}");
            }
            
            return await _supabase.ObtenerConfiguracionAsync();
        }
    }
}

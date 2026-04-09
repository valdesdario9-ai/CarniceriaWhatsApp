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
                
                // ✅ ACTUALIZAR DIRECTAMENTE VÍA HTTP PATCH
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
                }
                
                // ✅ GUARDAR EN SESIÓN
                HttpContext.Session.SetString("LicenciaActivada_" + mesActual, "true");
                
                Message = "✅ ¡Gracias! Licencia activada para este mes.";
                IsError = false;
                LicenciaActivadaEsteMes = true;
                MostrarBannerLicencia = false;
                
                System.Console.WriteLine($"[LICENCIA] ✅ Sesión guardada: LicenciaActivada_{mesActual}=true");
                System.Console.WriteLine("========================================");
                
                try { Productos = await _supabase.ObtenerProductosAsync(); } catch {}
                return Page();
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
        
        // ✅ ✅ ✅ MÉTODO NUEVO AGREGADO: ELIMINAR PRODUCTO ✅ ✅ ✅
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            System.Console.WriteLine($"[DELETE] Intentando eliminar producto ID: {id}");
            
            try
            {
                var resultado = await _supabase.EliminarProductoAsync(id);
                
                if (resultado)
                {
                    Message = "✅ Producto eliminado correctamente";
                    IsError = false;
                    System.Console.WriteLine($"[DELETE] ✅ Eliminado: {id}");
                }
                else
                {
                    Message = "❌ No se pudo eliminar el producto";
                    IsError = true;
                    System.Console.WriteLine($"[DELETE] ❌ Falló eliminación: {id}");
                }
            }
            catch (System.Exception ex)
            {
                Message = $"❌ Error: {ex.Message}";
                IsError = true;
                System.Console.WriteLine($"[DELETE] ❌ Exception: {ex.Message}");
            }
            
            // ✅ CRÍTICO: Recargar la lista de productos después de eliminar
            try { Productos = await _supabase.ObtenerProductosAsync(); } catch {}
            
            System.Console.WriteLine($"[DELETE] 📦 Productos recargados: {Productos?.Count ?? 0}");
            
            return Page(); // ✅ Mantener en la misma página con lista actualizada
        }
        // ✅ ✅ ✅ FIN DEL MÉTODO AGREGADO ✅ ✅ ✅
        
        private async Task VerificarEstadoLicencia()
        {
            try
            {
                var hoy = DateTime.Today;
                var diaActual = hoy.Day;
                var mesActual = $"{hoy.Year}-{hoy.Month:D2}";
                
                // ✅ 1. PRIMERO verificar sesión
                var licenciaEnSesion = HttpContext.Session.GetString("LicenciaActivada_" + mesActual);
                
                if (licenciaEnSesion == "true")
                {
                    System.Console.WriteLine($"[BANNER] 🔄 Sesión: licencia activada para {mesActual}");
                    MostrarBannerLicencia = false;
                    LicenciaActivadaEsteMes = true;
                    return;
                }
                
                // ✅ 2. Leer de BD con cache-busting
                var config = await LeerConfiguracionSinCacheAsync();
                
                System.Console.WriteLine($"[BANNER] 🔍 BD: pagada={config.LicenciaPagada}, hasta='{config.LicenciaPagadaHasta ?? "NULL"}', mes='{mesActual}'");
                
                bool licenciaAlDia = config.LicenciaPagada && config.LicenciaPagadaHasta == mesActual;
                
                // ✅ 3. Si está al día, guardar en sesión
                if (licenciaAlDia)
                {
                    HttpContext.Session.SetString("LicenciaActivada_" + mesActual, "true");
                    System.Console.WriteLine($"[BANNER] ✅ Sesión actualizada desde BD");
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
        
        // ✅ LECTURA DIRECTA SIN CACHE
        private async Task<ConfiguracionCarniceria> LeerConfiguracionSinCacheAsync()
        {
            try
            {
                var supabaseUrl = Environment.GetEnvironmentVariable("Supabase__Url") 
                    ?? "https://wfmoyssyoyqnxqjqedcc.supabase.co";
                var supabaseKey = Environment.GetEnvironmentVariable("Supabase__ApiKey");
                
                System.Console.WriteLine($"[CONFIG] 🔑 API Key configurada: {!string.IsNullOrEmpty(supabaseKey)}");
                
                if (string.IsNullOrEmpty(supabaseKey))
                {
                    System.Console.WriteLine($"[CONFIG] ⚠️ Sin API Key, usando fallback");
                    return await _supabase.ObtenerConfiguracionAsync();
                }
                
                // ✅ Cache-busting con timestamp único
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var url = $"{supabaseUrl}/rest/v1/configuracion_carniceria?select=*&order=id.asc&limit=1&_nocache={timestamp}";
                
                System.Console.WriteLine($"[CONFIG] 🔗 URL: {url}");
                
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("apikey", supabaseKey);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
                client.DefaultRequestHeaders.Add("Prefer", "return=representation");
                
                var response = await client.GetAsync(url);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                System.Console.WriteLine($"[CONFIG] 📡 Response: {response.StatusCode}");
                System.Console.WriteLine($"[CONFIG] 📄 Body: {responseBody.Substring(0, Math.Min(200, responseBody.Length))}...");
                
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
                        
                        System.Console.WriteLine($"[CONFIG] ✅ Parseado: pagada={config.LicenciaPagada}, hasta='{config.LicenciaPagadaHasta}'");
                        return config;
                    }
                    else
                    {
                        System.Console.WriteLine($"[CONFIG] ❌ JSON array vacío");
                    }
                }
                else
                {
                    System.Console.WriteLine($"[CONFIG] ❌ HTTP {response.StatusCode}");
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[CONFIG] ❌ Exception: {ex.Message}");
            }
            
            System.Console.WriteLine($"[CONFIG] ⚠️ Fallback a método del servicio");
            return await _supabase.ObtenerConfiguracionAsync();
        }
    }
}

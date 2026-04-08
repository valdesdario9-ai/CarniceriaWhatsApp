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
            
            Productos = await _supabase.ObtenerProductosAsync();
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
                Productos = await _supabase.ObtenerProductosAsync();
                return Page();
            }
            
            System.Console.WriteLine($"[LICENCIA] ✅ Clave maestra válida");
            
            try
            {
                var config = await _supabase.ObtenerConfiguracionAsync();
                var hoy = DateTime.Today;
                var mesActual = $"{hoy.Year}-{hoy.Month:D2}";
                
                var updateData = new
                {
                    licencia_pagada = true,
                    licencia_pagada_hasta = mesActual,
                    nota_licencia = $"Activado desde panel el {hoy:dd/MM/yyyy}",
                    actualizado_en = DateTime.UtcNow.ToString("o")
                };
                
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
                var json = JsonSerializer.Serialize(updateData, options);
                
                var supabaseUrl = Environment.GetEnvironmentVariable("Supabase__Url") 
                    ?? "https://wfmoyssyoyqnxqjqedcc.supabase.co";
                var supabaseKey = Environment.GetEnvironmentVariable("Supabase__ApiKey");
                
                if (string.IsNullOrEmpty(supabaseKey))
                {
                    Message = "❌ Error: API key no configurada";
                    IsError = true;
                    await VerificarEstadoLicencia();
                    Productos = await _supabase.ObtenerProductosAsync();
                    return Page();
                }
                
                var configId = config.Id ?? 1;
                var url = $"{supabaseUrl}/rest/v1/configuracion_carniceria?id=eq.{configId}";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
                _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync(url, content);
                var responseBody = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.GetArrayLength() > 0)
                    {
                        var updatedConfig = doc.RootElement[0];
                        var lp = updatedConfig.TryGetProperty("licencia_pagada", out var p) && p.GetBoolean();
                        var lh = updatedConfig.TryGetProperty("licencia_pagada_hasta", out var h) ? h.GetString() : null;
                        
                        System.Console.WriteLine($"[LICENCIA] ✅ PATCH: pagada={lp}, hasta='{lh}'");
                        
                        if (lp && lh == mesActual)
                        {
                            Message = "✅ ¡Gracias! Licencia activada para este mes.";
                            IsError = false;
                            LicenciaActivadaEsteMes = true;
                            MostrarBannerLicencia = false;
                            Productos = await _supabase.ObtenerProductosAsync();
                            
                            System.Console.WriteLine($"[LICENCIA] 🎉 Retornando Page() con estado forzado");
                            System.Console.WriteLine("========================================");
                            return Page();
                        }
                    }
                }
                else
                {
                    Message = $"❌ Error HTTP {response.StatusCode}";
                    IsError = true;
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[LICENCIA] ❌ Exception: {ex.Message}");
                Message = "❌ Error: " + ex.Message;
                IsError = true;
            }
            
            System.Console.WriteLine("========================================");
            await VerificarEstadoLicencia();
            Productos = await _supabase.ObtenerProductosAsync();
            return Page();
        }
        
        // ✅ CORREGIDO: VerificarEstadoLicencia con cache-busting
        private async Task VerificarEstadoLicencia()
        {
            try
            {
                // ✅ OBTENER CONFIGURACIÓN CON CACHE-BUSTING
                // Agregamos un timestamp único para forzar lectura fresca de Supabase
                var config = await ObtenerConfiguracionFrescaAsync();
                
                var hoy = DateTime.Today;
                var diaActual = hoy.Day;
                var mesActual = $"{hoy.Year}-{hoy.Month:D2}";
                
                System.Console.WriteLine($"[BANNER] Verificando: pagada={config.LicenciaPagada}, hasta='{config.LicenciaPagadaHasta ?? "NULL"}', mes='{mesActual}'");
                
                bool licenciaAlDia = config.LicenciaPagada && config.LicenciaPagadaHasta == mesActual;
                
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
        
        // ✅ NUEVO MÉTODO: Obtener configuración fresca con cache-busting
        private async Task<ConfiguracionCarniceria> ObtenerConfiguracionFrescaAsync()
        {
            try
            {
                var supabaseUrl = Environment.GetEnvironmentVariable("Supabase__Url") 
                    ?? "https://wfmoyssyoyqnxqjqedcc.supabase.co";
                var supabaseKey = Environment.GetEnvironmentVariable("Supabase__ApiKey");
                
                if (string.IsNullOrEmpty(supabaseKey))
                {
                    // Fallback al método del servicio si no hay API key
                    return await _supabase.ObtenerConfiguracionAsync();
                }
                
                // ✅ Agregar timestamp único para evitar cache
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
                        
                        // ✅ Mapear manualmente los campos (snake_case → PascalCase)
                        var config = new ConfiguracionCarniceria();
                        
                        if (element.TryGetProperty("id", out var id)) config.Id = id.GetInt64();
                        if (element.TryGetProperty("nombre_tienda", out var nt)) config.NombreTienda = nt.GetString();
                        if (element.TryGetProperty("descripcion", out var d)) config.Descripcion = d.GetString();
                        if (element.TryGetProperty("telefono", out var t)) config.Telefono = t.GetString();
                        if (element.TryGetProperty("whatsapp", out var w)) config.Whatsapp = w.GetString();
                        if (element.TryGetProperty("email", out var e)) config.Email = e.GetString();
                        if (element.TryGetProperty("direccion", out var dir)) config.Direccion = dir.GetString();
                        if (element.TryGetProperty("ciudad", out var c)) config.Ciudad = c.GetString();
                        if (element.TryGetProperty("provincia", out var p)) config.Provincia = p.GetString();
                        if (element.TryGetProperty("logo_url", out var lu)) config.LogoUrl = lu.GetString();
                        if (element.TryGetProperty("banner_url", out var bu)) config.BannerUrl = bu.GetString();
                        if (element.TryGetProperty("horarios", out var h)) config.Horarios = h.GetString();
                        if (element.TryGetProperty("alias_mercadopago", out var amp)) config.AliasMercadoPago = amp.GetString();
                        if (element.TryGetProperty("cbu", out var cbu)) config.Cbu = cbu.GetString();
                        if (element.TryGetProperty("instrucciones_pago", out var ip)) config.InstruccionesPago = ip.GetString();
                        
                        // ✅ Campos de licencia
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
                System.Console.WriteLine($"[CONFIG] ❌ Error lectura fresca: {ex.Message}");
            }
            
            // Fallback al método del servicio si falla la lectura directa
            return await _supabase.ObtenerConfiguracionAsync();
        }
    }
}

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
                Message = "❌ Clave de licencia incorrecta";
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
                
                System.Console.WriteLine($"[LICENCIA] 📋 Antes: pagada={config.LicenciaPagada}, hasta='{config.LicenciaPagadaHasta ?? "NULL"}'");
                
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
                
                System.Console.WriteLine($"[LICENCIA] 📡 Response: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    // ✅ SOLUCIÓN: Parsear la respuesta del PATCH para obtener los valores ACTUALIZADOS
                    // (en lugar de re-leer con ObtenerConfiguracionAsync que tiene cache)
                    using var doc = JsonDocument.Parse(responseBody);
                    if (doc.RootElement.GetArrayLength() > 0)
                    {
                        var updatedConfig = doc.RootElement[0];
                        
                        var licenciaPagadaActualizada = updatedConfig.TryGetProperty("licencia_pagada", out var lp) && lp.GetBoolean();
                        var licenciaHastaActualizada = updatedConfig.TryGetProperty("licencia_pagada_hasta", out var lh) ? lh.GetString() : null;
                        
                        System.Console.WriteLine($"[LICENCIA] ✅ Desde respuesta PATCH: pagada={licenciaPagadaActualizada}, hasta='{licenciaHastaActualizada}'");
                        
                        // ✅ Si la respuesta confirma la actualización, forzar estado local
                        if (licenciaPagadaActualizada && licenciaHastaActualizada == mesActual)
                        {
                            Message = "✅ ¡Gracias! Licencia activada para este mes. No volverá a aparecer hasta el próximo mes.";
                            IsError = false;
                            LicenciaActivadaEsteMes = true;
                            MostrarBannerLicencia = false;
                            
                            System.Console.WriteLine($"[LICENCIA] 🎉 Estado local forzado - Redirigiendo...");
                            System.Console.WriteLine("========================================");
                            return RedirectToPage("/Admin/Productos");
                        }
                    }
                    
                    // Fallback: si no se pudo parsear, intentar re-leer (aunque puede tener cache)
                    System.Console.WriteLine($"[LICENCIA] ⚠️ Fallback: re-leyendo configuración...");
                    var configVerificada = await _supabase.ObtenerConfiguracionAsync();
                    System.Console.WriteLine($"[LICENCIA] 🔍 Re-lectura: pagada={configVerificada.LicenciaPagada}, hasta='{configVerificada.LicenciaPagadaHasta}'");
                    
                    if (configVerificada.LicenciaPagada && configVerificada.LicenciaPagadaHasta == mesActual)
                    {
                        Message = "✅ ¡Gracias! Licencia activada para este mes.";
                        IsError = false;
                        LicenciaActivadaEsteMes = true;
                        MostrarBannerLicencia = false;
                        return RedirectToPage("/Admin/Productos");
                    }
                    else
                    {
                        Message = "⚠️ Licencia activada en BD, pero hay delay en la lectura. Recargá la página en unos segundos.";
                        IsError = false;
                    }
                }
                else
                {
                    System.Console.WriteLine($"[LICENCIA] ❌ Error HTTP {response.StatusCode}: {responseBody}");
                    Message = $"❌ Error ({response.StatusCode})";
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
        
        private async Task VerificarEstadoLicencia()
        {
            try
            {
                var config = await _supabase.ObtenerConfiguracionAsync();
                var hoy = DateTime.Today;
                var diaActual = hoy.Day;
                var mesActual = $"{hoy.Year}-{hoy.Month:D2}";
                
                System.Console.WriteLine($"[BANNER] 🔍 Verificando: pagada={config.LicenciaPagada}, hasta='{config.LicenciaPagadaHasta ?? "NULL"}', mes='{mesActual}'");
                
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
    }
}

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
        private readonly IHttpClientFactory _httpClientFactory;
        
        // 🔑 CLAVE MAESTRA
        private const string MASTER_KEY = "DEV_MASTER_KEY_2026_CARNICERIA_DV7x9Kp2";
        
        public ProductosModel(ISupabaseService supabase, IHttpClientFactory httpClientFactory)
        {
            _supabase = supabase;
            _httpClientFactory = httpClientFactory;
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
            
            if (TempData["LicenseWarning"] != null)
            {
                MostrarBannerLicencia = true;
                MensajeBannerLicencia = TempData["LicenseWarning"].ToString();
            }
            
            return Page();
        }
        
        // ✅ HANDLER: Activar licencia con actualización DIRECTA vía HTTP
        public async Task<IActionResult> OnPostActivarLicenciaAsync(string MasterKey)
        {
            System.Console.WriteLine("========================================");
            System.Console.WriteLine($"[LICENCIA] Intento de activación");
            
            // ✅ 1. Verificar clave maestra
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
                // ✅ 2. Obtener configuración actual para tener el ID
                var config = await _supabase.ObtenerConfiguracionAsync();
                var hoy = DateTime.Today;
                var mesActual = $"{hoy.Year}-{hoy.Month:D2}";
                
                System.Console.WriteLine($"[LICENCIA] Config ID: {config.Id ?? 0}");
                System.Console.WriteLine($"[LICENCIA] Actualizando a: licencia_pagada=true, hasta={mesActual}");
                
                // ✅ 3. Preparar datos para actualizar (snake_case para Supabase)
                var updateData = new
                {
                    licencia_pagada = true,
                    licencia_pagada_hasta = mesActual,
                    nota_licencia = $"Activado desde panel el {hoy:dd/MM/yyyy}",
                    actualizado_en = DateTime.UtcNow.ToString("o")
                };
                
                // ✅ 4. Serializar a JSON
                var options = new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    WriteIndented = false 
                };
                var json = JsonSerializer.Serialize(updateData, options);
                
                System.Console.WriteLine($"[LICENCIA] JSON a enviar: {json}");
                
                // ✅ 5. Obtener URL y API Key de Supabase
                var supabaseUrl = Environment.GetEnvironmentVariable("Supabase__Url") 
                    ?? "https://wfmoyssyoyqnxqjqedcc.supabase.co";
                var supabaseKey = Environment.GetEnvironmentVariable("Supabase__ApiKey");
                
                if (string.IsNullOrEmpty(supabaseKey))
                {
                    System.Console.WriteLine($"[LICENCIA] ❌ ERROR: No hay Supabase API Key");
                    Message = "❌ Error: API key de Supabase no configurada";
                    IsError = true;
                    await VerificarEstadoLicencia();
                    return Page();
                }
                
                // ✅ 6. Hacer PATCH directo a Supabase REST API
                var configId = config.Id ?? 1;  // Fallback a 1 si es null
                var url = $"{supabaseUrl}/rest/v1/configuracion_carniceria?id=eq.{configId}";
                
                using (var client = _httpClientFactory.CreateClient())
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("apikey", supabaseKey);
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
                    client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                    client.DefaultRequestHeaders.Add("Prefer", "return=representation");
                    
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PatchAsync(url, content);
                    
                    var responseBody = await response.Content.ReadAsStringAsync();
                    
                    System.Console.WriteLine($"[LICENCIA] Response Status: {response.StatusCode}");
                    System.Console.WriteLine($"[LICENCIA] Response Body: {responseBody}");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        System.Console.WriteLine($"[LICENCIA] ✅ BD ACTUALIZADA EXITOSAMENTE");
                        
                        // ✅ 7. Verificar que se actualizó leyendo de nuevo
                        var configVerificada = await _supabase.ObtenerConfiguracionAsync();
                        System.Console.WriteLine($"[LICENCIA] Verificación: licencia_pagada={configVerificada.LicenciaPagada}, hasta='{configVerificada.LicenciaPagadaHasta}'");
                        
                        Message = "✅ ¡Gracias! Licencia activada para este mes. No volverá a aparecer hasta el próximo mes.";
                        IsError = false;
                        LicenciaActivadaEsteMes = true;
                        MostrarBannerLicencia = false;
                        
                        // ✅ 8. Redirigir para limpiar estado
                        return RedirectToPage("/Admin/Productos");
                    }
                    else
                    {
                        System.Console.WriteLine($"[LICENCIA] ❌ Error HTTP {response.StatusCode}: {responseBody}");
                        Message = $"❌ Error al activar ({response.StatusCode}): {responseBody}";
                        IsError = true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[LICENCIA] ❌ Exception: {ex.Message}");
                System.Console.WriteLine($"[LICENCIA] Stack: {ex.StackTrace}");
                Message = "❌ Error: " + ex.Message;
                IsError = true;
            }
            
            // Si llegó acá, hubo error
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
                
                System.Console.WriteLine($"[BANNER] Verificando: pagada={config.LicenciaPagada}, hasta='{config.LicenciaPagadaHasta}', mesActual='{mesActual}'");
                
                bool licenciaAlDia = config.LicenciaPagada && config.LicenciaPagadaHasta == mesActual;
                
                System.Console.WriteLine($"[BANNER] licenciaAlDia = {licenciaAlDia}");
                
                if (!licenciaAlDia && diaActual >= 1 && diaActual <= 10)
                {
                    var diasRestantes = 10 - diaActual;
                    MensajeBannerLicencia = $"⚠️ Recordatorio: Tu licencia vence el 10 de {hoy:MMMM}. Te quedan {diasRestantes} días para regularizar.";
                    MostrarBannerLicencia = true;
                    LicenciaActivadaEsteMes = false;
                    System.Console.WriteLine($"[BANNER] ✅ Mostrando banner amarillo");
                }
                else if (licenciaAlDia)
                {
                    MostrarBannerLicencia = false;
                    LicenciaActivadaEsteMes = true;
                    System.Console.WriteLine($"[BANNER] ✅ Licencia al día - Sin banner amarillo");
                }
                else
                {
                    MostrarBannerLicencia = false;
                    LicenciaActivadaEsteMes = false;
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[BANNER] Error: {ex.Message}");
            }
        }
    }
}

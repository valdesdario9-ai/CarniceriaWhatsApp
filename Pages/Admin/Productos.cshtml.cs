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
        
        // 🔑 CLAVE MAESTRA
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
        
        // ✅ CORREGIDO: OnGetAsync
        public async Task<IActionResult> OnGetAsync()
        {
            if (HttpContext.Session.GetString("AdminLogged") != "true")
                return RedirectToPage("/Admin/Login");
            
            Productos = await _supabase.ObtenerProductosAsync();
            
            // ✅ 1. Primero verificar estado de licencia (esto es lo importante)
            await VerificarEstadoLicencia();
            
            // ✅ 2. Solo mostrar TempData si la licencia NO está activada este mes
            // (evita que un mensaje viejo override el estado actual)
            if (TempData["LicenseWarning"] != null && !LicenciaActivadaEsteMes)
            {
                MostrarBannerLicencia = true;
                MensajeBannerLicencia = TempData["LicenseWarning"].ToString();
            }
            
            return Page();
        }
        
        // ✅ HANDLER: Activar licencia con actualización DIRECTA
        public async Task<IActionResult> OnPostActivarLicenciaAsync(string MasterKey)
        {
            System.Console.WriteLine("========================================");
            System.Console.WriteLine($"[LICENCIA] 🚀 Intento de activación");
            
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
                // ✅ 2. Obtener configuración actual
                var config = await _supabase.ObtenerConfiguracionAsync();
                var hoy = DateTime.Today;
                var mesActual = $"{hoy.Year}-{hoy.Month:D2}";
                
                System.Console.WriteLine($"[LICENCIA] 📋 Config actual:");
                System.Console.WriteLine($"   - Id: {config.Id ?? 0}");
                System.Console.WriteLine($"   - licencia_pagada: {config.LicenciaPagada}");
                System.Console.WriteLine($"   - licencia_pagada_hasta: '{config.LicenciaPagadaHasta ?? "NULL"}'");
                System.Console.WriteLine($"[LICENCIA] 🎯 Actualizando a: licencia_pagada=true, hasta={mesActual}");
                
                // ✅ 3. Preparar datos para Supabase (snake_case)
                var updateData = new
                {
                    licencia_pagada = true,
                    licencia_pagada_hasta = mesActual,
                    nota_licencia = $"Activado desde panel el {hoy:dd/MM/yyyy}",
                    actualizado_en = DateTime.UtcNow.ToString("o")
                };
                
                var options = new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
                };
                var json = JsonSerializer.Serialize(updateData, options);
                
                System.Console.WriteLine($"[LICENCIA] 📦 JSON: {json}");
                
                // ✅ 4. Obtener URL y API Key
                var supabaseUrl = Environment.GetEnvironmentVariable("Supabase__Url") 
                    ?? "https://wfmoyssyoyqnxqjqedcc.supabase.co";
                var supabaseKey = Environment.GetEnvironmentVariable("Supabase__ApiKey");
                
                if (string.IsNullOrEmpty(supabaseKey))
                {
                    System.Console.WriteLine($"[LICENCIA] ❌ ERROR: No hay Supabase API Key");
                    Message = "❌ Error: API key no configurada";
                    IsError = true;
                    await VerificarEstadoLicencia();
                    return Page();
                }
                
                // ✅ 5. Hacer PATCH directo
                var configId = config.Id ?? 1;
                var url = $"{supabaseUrl}/rest/v1/configuracion_carniceria?id=eq.{configId}";
                
                System.Console.WriteLine($"[LICENCIA] 🔗 URL: {url}");
                
                // ✅ Limpiar headers previos y agregar SOLO los de request
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
                _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");
                
                // ✅ StringContent ya establece Content-Type automáticamente
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PatchAsync(url, content);
                
                var responseBody = await response.Content.ReadAsStringAsync();
                
                System.Console.WriteLine($"[LICENCIA] 📡 Response: {response.StatusCode}");
                System.Console.WriteLine($"[LICENCIA] 📄 Body: {responseBody}");
                
                if (response.IsSuccessStatusCode)
                {
                    System.Console.WriteLine($"[LICENCIA] ✅ BD ACTUALIZADA EXITOSAMENTE");
                    
                    // ✅ 6. Verificar lectura post-update
                    var configVerificada = await _supabase.ObtenerConfiguracionAsync();
                    System.Console.WriteLine($"[LICENCIA] 🔍 Verificación:");
                    System.Console.WriteLine($"   - licencia_pagada: {configVerificada.LicenciaPagada}");
                    System.Console.WriteLine($"   - licencia_pagada_hasta: '{configVerificada.LicenciaPagadaHasta}'");
                    
                    Message = "✅ ¡Gracias! Licencia activada para este mes. No volverá a aparecer hasta el próximo mes.";
                    IsError = false;
                    LicenciaActivadaEsteMes = true;
                    MostrarBannerLicencia = false;
                    
                    System.Console.WriteLine($"[LICENCIA] 🎉 Redirigiendo...");
                    System.Console.WriteLine("========================================");
                    return RedirectToPage("/Admin/Productos");
                }
                else
                {
                    System.Console.WriteLine($"[LICENCIA] ❌ Error HTTP {response.StatusCode}: {responseBody}");
                    Message = $"❌ Error ({response.StatusCode}): {responseBody}";
                    IsError = true;
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[LICENCIA] ❌ Exception: {ex.Message}");
                System.Console.WriteLine($"[LICENCIA] 📋 Stack: {ex.StackTrace}");
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
                
                System.Console.WriteLine($"[BANNER] 🔍 Verificando estado:");
                System.Console.WriteLine($"   - Día actual: {diaActual}");
                System.Console.WriteLine($"   - Mes actual: {mesActual}");
                System.Console.WriteLine($"   - licencia_pagada: {config.LicenciaPagada}");
                System.Console.WriteLine($"   - licencia_pagada_hasta: '{config.LicenciaPagadaHasta ?? "NULL"}'");
                
                bool licenciaAlDia = config.LicenciaPagada && config.LicenciaPagadaHasta == mesActual;
                
                System.Console.WriteLine($"[BANNER] ✅ licenciaAlDia = {licenciaAlDia}");
                
                if (!licenciaAlDia && diaActual >= 1 && diaActual <= 10)
                {
                    var diasRestantes = 10 - diaActual;
                    MensajeBannerLicencia = $"⚠️ Recordatorio: Tu licencia vence el 10 de {hoy:MMMM}. Te quedan {diasRestantes} días para regularizar.";
                    MostrarBannerLicencia = true;
                    LicenciaActivadaEsteMes = false;
                    System.Console.WriteLine($"[BANNER] 🟡 Mostrando banner amarillo");
                }
                else if (licenciaAlDia)
                {
                    MostrarBannerLicencia = false;
                    LicenciaActivadaEsteMes = true;
                    System.Console.WriteLine($"[BANNER] 🟢 Licencia al día - Sin banner amarillo");
                }
                else
                {
                    MostrarBannerLicencia = false;
                    LicenciaActivadaEsteMes = false;
                    System.Console.WriteLine($"[BANNER] ⚪ Fuera de período de recordatorio");
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[BANNER] ❌ Error: {ex.Message}");
            }
        }
    }
}

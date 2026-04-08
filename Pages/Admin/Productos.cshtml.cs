using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using System.Collections.Generic;
using CarniceriaWhatsApp.Models;
using CarniceriaWhatsApp.Services;

namespace CarniceriaWhatsApp.Pages.Admin
{
    public class ProductosModel : PageModel
    {
        private readonly ISupabaseService _supabase;
        
        // 🔑 CLAVE MAESTRA (MISMA que en Login.cshtml.cs)
        private const string MASTER_KEY = "DEV_MASTER_KEY_2026_CARNICERIA_DV7x9Kp2";
        
        public ProductosModel(ISupabaseService supabase)
        {
            _supabase = supabase;
        }
        
        public List<Producto> Productos { get; set; } = new List<Producto>();
        public string Message { get; set; } = "";
        public bool IsError { get; set; }
        
        // ✅ Propiedades para el banner de licencia
        public bool MostrarBannerLicencia { get; set; } = false;
        public string MensajeBannerLicencia { get; set; } = "";
        public bool LicenciaActivadaEsteMes { get; set; } = false;
        
        public async Task<IActionResult> OnGetAsync()
        {
            // ✅ Verificar sesión de admin
            if (HttpContext.Session.GetString("AdminLogged") != "true")
            {
                return RedirectToPage("/Admin/Login");
            }
            
            // ✅ Obtener productos
            Productos = await _supabase.ObtenerProductosAsync();
            
            // ✅ Verificar estado de licencia para banner
            await VerificarEstadoLicencia();
            
            // ✅ Mostrar mensaje de TempData si viene del login
            if (TempData["LicenseWarning"] != null)
            {
                MostrarBannerLicencia = true;
                MensajeBannerLicencia = TempData["LicenseWarning"].ToString();
            }
            
            return Page();
        }
        
        // ✅ HANDLER PARA ACTIVAR LICENCIA CON CLAVE MAESTRA
        public async Task<IActionResult> OnPostActivarLicenciaAsync(string MasterKey)
        {
            System.Console.WriteLine($"[LICENCIA] Intento de activación con clave: {!string.IsNullOrEmpty(MasterKey)}");
            
            // ✅ Verificar clave maestra
            if (string.IsNullOrEmpty(MasterKey) || MasterKey != MASTER_KEY)
            {
                Message = "❌ Clave de licencia incorrecta";
                IsError = true;
                await VerificarEstadoLicencia();
                Productos = await _supabase.ObtenerProductosAsync();
                return Page();
            }
            
            // ✅ Clave correcta → Actualizar BD
            try
            {
                var config = await _supabase.ObtenerConfiguracionAsync();
                var hoy = DateTime.Today;
                var mesActual = $"{hoy.Year}-{hoy.Month:D2}";
                
                config.LicenciaPagada = true;
                config.LicenciaPagadaHasta = mesActual;
                config.NotaLicencia = $"Activado desde panel el {hoy:dd/MM/yyyy}";
                
                await _supabase.ActualizarConfiguracionAsync(config);
                
                System.Console.WriteLine($"[LICENCIA] ✅ BD actualizada: licencia_pagada=true, hasta={mesActual}");
                
                Message = "✅ Licencia activada para este mes";
                IsError = false;
                LicenciaActivadaEsteMes = true;
                
                // ✅ Redirigir para limpiar TempData y recargar estado
                return RedirectToPage("/Admin/Productos");
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[LICENCIA] ❌ Error: {ex.Message}");
                Message = "❌ Error al activar licencia: " + ex.Message;
                IsError = true;
                await VerificarEstadoLicencia();
                Productos = await _supabase.ObtenerProductosAsync();
                return Page();
            }
        }
        
        // ✅ Método auxiliar para verificar estado de licencia
        private async Task VerificarEstadoLicencia()
        {
            try
            {
                var config = await _supabase.ObtenerConfiguracionAsync();
                var hoy = DateTime.Today;
                var diaActual = hoy.Day;
                var mesActual = $"{hoy.Year}-{hoy.Month:D2}";
                
                bool licenciaAlDia = config.LicenciaPagada && config.LicenciaPagadaHasta == mesActual;
                
                // 📅 Días 1-10 + No pagada → Mostrar banner
                if (!licenciaAlDia && diaActual >= 1 && diaActual <= 10)
                {
                    var diasRestantes = 10 - diaActual;
                    MensajeBannerLicencia = $"⚠️ Recordatorio: Tu licencia vence el 10 de {hoy:MMMM}. Te quedan {diasRestantes} días para regularizar.";
                    MostrarBannerLicencia = true;
                }
                // ✅ Licencia al día → No mostrar banner
                else if (licenciaAlDia)
                {
                    MostrarBannerLicencia = false;
                    LicenciaActivadaEsteMes = true;
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[LICENCIA] Error al verificar: {ex.Message}");
            }
        }
    }
}

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
        private const string MASTER_KEY = "DEV_MASTER_KEY_2026_CARNICERIA_DV7x9Kp2";
        
        public ProductosModel(ISupabaseService supabase)
        {
            _supabase = supabase;
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
            if (string.IsNullOrEmpty(MasterKey) || MasterKey != MASTER_KEY)
            {
                Message = "❌ Clave incorrecta"; IsError = true;
                await VerificarEstadoLicencia();
                try { Productos = await _supabase.ObtenerProductosAsync(); } catch {}
                return Page();
            }
            
            try
            {
                var config = await _supabase.ObtenerConfiguracionAsync();
                var hoy = DateTime.Today;
                var mesActual = $"{hoy.Year}-{hoy.Month:D2}";
                
                config.LicenciaPagada = true;
                config.LicenciaPagadaHasta = mesActual;
                config.NotaLicencia = $"Activado el {hoy:dd/MM/yyyy}";
                
                await _supabase.ActualizarConfiguracionAsync(config);
                
                Message = "✅ ¡Gracias! Licencia activada para este mes.";
                IsError = false;
                LicenciaActivadaEsteMes = true;
                MostrarBannerLicencia = false;
                
                try { Productos = await _supabase.ObtenerProductosAsync(); } catch {}
                return Page(); // ✅ No redirigir para evitar re-lectura con cache
            }
            catch (System.Exception ex)
            {
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
                var config = await _supabase.ObtenerConfiguracionAsync();
                var hoy = DateTime.Today;
                var diaActual = hoy.Day;
                var mesActual = $"{hoy.Year}-{hoy.Month:D2}";
                
                bool licenciaAlDia = config.LicenciaPagada && config.LicenciaPagadaHasta == mesActual;
                
                if (!licenciaAlDia && diaActual >= 1 && diaActual <= 10)
                {
                    var diasRestantes = 10 - diaActual;
                    MensajeBannerLicencia = $"⚠️ Recordatorio: Tu licencia vence el 10 de {hoy:MMMM}. Te quedan {diasRestantes} días para regularizar.";
                    MostrarBannerLicencia = true;
                    LicenciaActivadaEsteMes = false;
                }
                else if (licenciaAlDia)
                {
                    MostrarBannerLicencia = false;
                    LicenciaActivadaEsteMes = true;
                }
                else
                {
                    MostrarBannerLicencia = false;
                    LicenciaActivadaEsteMes = false;
                }
            }
            catch { /* Silenciar errores para no romper la app */ }
        }
    }
}

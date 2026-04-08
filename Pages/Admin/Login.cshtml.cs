using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using CarniceriaWhatsApp.Services;
using CarniceriaWhatsApp.Models;

namespace CarniceriaWhatsApp.Pages.Admin
{
    public class LoginModel : PageModel
    {
        private readonly ISupabaseService _supabase;
        
        public string Message { get; set; } = "";
        public bool BloqueadoPorLicencia { get; set; } = false;
        public string MensajeBloqueo { get; set; } = "";
        
        public LoginModel(ISupabaseService supabase)
        {
            _supabase = supabase;
        }
        
        public IActionResult OnGet() => Page();
        
        public async Task<IActionResult> OnPostAsync(string Username, string Password)
        {
            if (Username != "admin" || Password != "admin123")
            {
                Message = "❌ Usuario o contraseña incorrectos";
                return Page();
            }
            
            var config = await _supabase.ObtenerConfiguracionAsync();
            var hoy = DateTime.Today;
            var diaActual = hoy.Day;
            var mesActual = $"{hoy.Year}-{hoy.Month:D2}";
            
            if (diaActual >= 1 && diaActual <= 10)
            {
                if (!config.LicenciaPagada || config.LicenciaPagadaHasta != mesActual)
                {
                    BloqueadoPorLicencia = true;
                    var diasRestantes = 10 - diaActual;
                    MensajeBloqueo = $"⚠️ Recordatorio de Licencia\n\nEl pago de la licencia es mensual y debe realizarse entre el 1° y 10° de cada mes.\n\n📅 Hoy es {hoy:dd 'de' MMMM} - Te quedan {diasRestantes} día{(diasRestantes > 1 ? "s" : "")} para regularizar.\n\n💬 Contactanos por WhatsApp para coordinar el pago.";
                    return Page();
                }
            }
            
            HttpContext.Session.SetString("AdminLogged", "true");
            return RedirectToPage("/Admin/Productos");
        }
    }
}

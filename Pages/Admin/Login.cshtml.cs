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
        
        // ✅ CLAVE MAESTRA DEL DESARROLLADOR (CAMBIAR por tu clave real)
        private const string MASTER_KEY = "DEV_MASTER_KEY_2026_CARNICERIA_DV7x9Kp2";
        
        public string Message { get; set; } = "";
        public bool BloqueadoPorLicencia { get; set; } = false;
        public string MensajeBloqueo { get; set; } = "";
        
        public LoginModel(ISupabaseService supabase)
        {
            _supabase = supabase;
        }
        
        public IActionResult OnGet() => Page();
        
        public async Task<IActionResult> OnPostAsync(string Username, string Password, string MasterKey)
        {
            // ✅ 1. Verificar credenciales básicas
            if (Username != "admin" || Password != "admin123")
            {
                Message = "❌ Usuario o contraseña incorrectos";
                return Page();
            }
            
            // ✅ 2. Verificar CLAVE MAESTRA (bypass de licencia)
            bool claveMaestraValida = !string.IsNullOrEmpty(MasterKey) && MasterKey == MASTER_KEY;
            
            // ✅ 3. Obtener configuración para verificar estado de licencia
            var config = await _supabase.ObtenerConfiguracionAsync();
            var hoy = DateTime.Today;
            var diaActual = hoy.Day;
            var mesActual = $"{hoy.Year}-{hoy.Month:D2}";  // Ej: "2026-04"
            
            // ✅ 4. Lógica de bloqueo por licencia (solo días 1-10 del mes)
            if (diaActual >= 1 && diaActual <= 10)
            {
                if (!config.LicenciaPagada || config.LicenciaPagadaHasta != mesActual)
                {
                    // ❌ Licencia NO pagada
                    if (!claveMaestraValida)
                    {
                        // 🚫 Sin clave maestra → BLOQUEAR ACCESO
                        BloqueadoPorLicencia = true;
                        
                        var diasRestantes = 10 - diaActual;
                        MensajeBloqueo = $"⚠️ Recordatorio de Licencia\n\n" +
                            $"El pago de la licencia es mensual y debe realizarse entre el 1° y 10° de cada mes.\n\n" +
                            $"📅 Hoy es {hoy:dd 'de' MMMM} - Te quedan {diasRestantes} día{(diasRestantes > 1 ? "s" : "")} para regularizar.\n\n" +
                            $"💬 Contactá al desarrollador para coordinar el pago.";
                        
                        return Page();
                    }
                    // ✅ Con clave maestra → CONTINUAR (bypass)
                }
            }
            // 📅 Días 11-31: Período de gracia, acceso permitido siempre
            
            // ✅ 5. Login exitoso → Crear sesión y redirigir
            HttpContext.Session.SetString("AdminLogged", "true");
            
            // ✅ Si se usó clave maestra, mostrar mensaje informativo
            if (claveMaestraValida)
            {
                Message = "🔓 Acceso habilitado con clave de desarrollador";
            }
            
            return RedirectToPage("/Admin/Productos");
        }
    }
}

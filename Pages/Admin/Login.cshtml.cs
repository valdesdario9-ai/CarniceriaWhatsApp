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
        public bool MostrarRecordatorio { get; set; } = false;
        public string MensajeRecordatorio { get; set; } = "";
        
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
            
            // ✅ 2. Verificar CLAVE MAESTRA (bypass total de licencia)
            bool claveMaestraValida = !string.IsNullOrEmpty(MasterKey) && MasterKey == MASTER_KEY;
            
            // ✅ 3. Obtener configuración para verificar estado de licencia
            var config = await _supabase.ObtenerConfiguracionAsync();
            var hoy = DateTime.Today;
            var diaActual = hoy.Day;
            var mesActual = $"{hoy.Year}-{hoy.Month:D2}";  // Ej: "2026-04"
            
            // ✅ 4. Lógica de licencia CORREGIDA
            if (!config.LicenciaPagada || config.LicenciaPagadaHasta != mesActual)
            {
                // ❌ Licencia NO pagada para este mes
                
                if (diaActual >= 1 && diaActual <= 10)
                {
                    // 📅 Días 1-10: SOLO MOSTRAR RECORDATORIO (pero permitir acceso)
                    if (!claveMaestraValida)
                    {
                        MostrarRecordatorio = true;
                        var diasRestantes = 10 - diaActual;
                        MensajeRecordatorio = $"⚠️ Recordatorio: Tu licencia vence el 10 de {hoy:MMMM}. Te quedan {diasRestantes} día{(diasRestantes > 1 ? "s" : "")} para regularizar.";
                    }
                    // ✅ Con clave maestra → Acceso sin recordatorio
                }
                else if (diaActual > 10)
                {
                    // 📅 Día 11+: BLOQUEAR ACCESO (a menos que tenga clave maestra)
                    if (!claveMaestraValida)
                    {
                        BloqueadoPorLicencia = true;
                        var diasVencido = diaActual - 10;
                        MensajeBloqueo = $"❌ Licencia Vencida\n\n" +
                            $"El pago de la licencia debía realizarse entre el 1° y 10° de {hoy:MMMM}.\n\n" +
                            $"📅 Hoy es {hoy:dd 'de' MMMM} - Tu acceso está bloqueado hace {diasVencido} día{(diasVencido > 1 ? "s" : "")}.\n\n" +
                            $"💬 Contactá al desarrollador para regularizar tu situación.";
                        return Page();
                    }
                    // ✅ Con clave maestra → Acceso permitido
                }
            }
            // ✅ Licencia pagada → Acceso normal sin mensajes
            
            // ✅ 5. Login exitoso → Crear sesión y redirigir
            HttpContext.Session.SetString("AdminLogged", "true");
            
            // ✅ Si se usó clave maestra, mostrar mensaje
            if (claveMaestraValida)
            {
                Message = "🔓 Acceso habilitado con clave de desarrollador";
            }
            
            return RedirectToPage("/Admin/Productos");
        }
    }
}

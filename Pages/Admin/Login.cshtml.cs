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
            System.Console.WriteLine("========================================");
            System.Console.WriteLine($"[LOGIN] Intento de login - Usuario: {Username}");
            System.Console.WriteLine($"[LOGIN] Fecha del servidor: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            
            // ✅ 1. Verificar credenciales básicas
            if (Username != "admin" || Password != "admin123")
            {
                Message = "❌ Usuario o contraseña incorrectos";
                System.Console.WriteLine($"[LOGIN] Credenciales incorrectas");
                return Page();
            }
            
            System.Console.WriteLine($"[LOGIN] ✅ Credenciales correctas");
            
            // ✅ 2. Verificar CLAVE MAESTRA
            bool claveMaestraValida = !string.IsNullOrEmpty(MasterKey) && MasterKey == MASTER_KEY;
            System.Console.WriteLine($"[LOGIN] Clave maestra proporcionada: {!string.IsNullOrEmpty(MasterKey)}");
            System.Console.WriteLine($"[LOGIN] Clave maestra válida: {claveMaestraValida}");
            
            // ✅ 3. Obtener configuración
            System.Console.WriteLine($"[LICENCIA] Obteniendo configuración desde Supabase...");
            var config = await _supabase.ObtenerConfiguracionAsync();
            
            var hoy = DateTime.Today;
            var diaActual = hoy.Day;
            var mesActual = $"{hoy.Year}-{hoy.Month:D2}";
            
            System.Console.WriteLine($"[LICENCIA] ========================================");
            System.Console.WriteLine($"[LICENCIA] Hoy: {hoy:dd/MM/yyyy}");
            System.Console.WriteLine($"[LICENCIA] Día actual: {diaActual}");
            System.Console.WriteLine($"[LICENCIA] Mes actual: {mesActual}");
            System.Console.WriteLine($"[LICENCIA] Licencia pagada (desde BD): {config.LicenciaPagada}");
            System.Console.WriteLine($"[LICENCIA] Licencia pagada hasta (desde BD): '{config.LicenciaPagadaHasta ?? "NULL"}'");
            System.Console.WriteLine($"[LICENCIA] ========================================");
            
            // ✅ 4. Lógica de licencia
            bool licenciaAlDia = config.LicenciaPagada && config.LicenciaPagadaHasta == mesActual;
            System.Console.WriteLine($"[LICENCIA] ¿Licencia al día? {licenciaAlDia}");
            
            if (!licenciaAlDia)
            {
                System.Console.WriteLine($"[LICENCIA] ⚠️ Licencia NO está al día");
                
                if (diaActual >= 1 && diaActual <= 10)
                {
                    System.Console.WriteLine($"[LICENCIA] 📅 Estamos en días 1-10 (período de recordatorio)");
                    
                    if (!claveMaestraValida)
                    {
                        MostrarRecordatorio = true;
                        var diasRestantes = 10 - diaActual;
                        MensajeRecordatorio = $"⚠️ Recordatorio: Tu licencia vence el 10 de {hoy:MMMM}. Te quedan {diasRestantes} día{(diasRestantes > 1 ? "s" : "")} para regularizar.";
                        
                        System.Console.WriteLine($"[LICENCIA] ✅ MOSTRANDO RECORDATORIO: {MensajeRecordatorio}");
                        System.Console.WriteLine($"[LICENCIA] ✅ MostrarRecordatorio = true");
                    }
                    else
                    {
                        System.Console.WriteLine($"[LICENCIA] 🔓 Clave maestra válida - Sin recordatorio");
                    }
                }
                else if (diaActual > 10)
                {
                    System.Console.WriteLine($"[LICENCIA] 📅 Estamos en día {diaActual} (período de BLOQUEO)");
                    
                    if (!claveMaestraValida)
                    {
                        BloqueadoPorLicencia = true;
                        var diasVencido = diaActual - 10;
                        MensajeBloqueo = $"❌ Licencia Vencida\n\nEl pago debía realizarse entre el 1° y 10° de {hoy:MMMM}.\n\n📅 Hoy es {hoy:dd 'de' MMMM} - Tu acceso está bloqueado hace {diasVencido} día{(diasVencido > 1 ? "s" : "")}.\n\n💬 Contactá al desarrollador para regularizar.";
                        
                        System.Console.WriteLine($"[LICENCIA] ❌ BLOQUEADO: {MensajeBloqueo}");
                        System.Console.WriteLine($"[LICENCIA] ❌ BloqueadoPorLicencia = true");
                        return Page();
                    }
                    else
                    {
                        System.Console.WriteLine($"[LICENCIA] 🔓 Clave maestra válida - Acceso permitido");
                    }
                }
            }
            else
            {
                System.Console.WriteLine($"[LICENCIA] ✅ Licencia al día - Sin mensajes");
            }
            
            // ✅ 5. Login exitoso
            HttpContext.Session.SetString("AdminLogged", "true");
            
            if (claveMaestraValida)
            {
                Message = "🔓 Acceso habilitado con clave de desarrollador";
            }
            
            System.Console.WriteLine($"[LOGIN] ✅ Login exitoso - Redirigiendo a Productos");
            System.Console.WriteLine($"[LOGIN] MostrarRecordatorio = {MostrarRecordatorio}");
            System.Console.WriteLine($"[LOGIN] MensajeRecordatorio = {MensajeRecordatorio}");
            System.Console.WriteLine("========================================");
            
            return RedirectToPage("/Admin/Productos");
        }
    }
}

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
        
        public LoginModel(ISupabaseService supabase)
        {
            _supabase = supabase;
        }
        
        public IActionResult OnGet() => Page();
        
        public async Task<IActionResult> OnPostAsync(string Username, string Password, string MasterKey)
        {
            System.Console.WriteLine($"[LOGIN] Intento de login - Usuario: {Username}");
            
            // ✅ 1. Verificar credenciales básicas
            if (Username != "admin" || Password != "admin123")
            {
                Message = "❌ Usuario o contraseña incorrectos";
                return Page();
            }
            
            System.Console.WriteLine($"[LOGIN] ✅ Credenciales correctas");
            
            // ✅ 2. Verificar CLAVE MAESTRA
            bool claveMaestraValida = !string.IsNullOrEmpty(MasterKey) && MasterKey == MASTER_KEY;
            
            // ✅ 3. Obtener configuración
            var config = await _supabase.ObtenerConfiguracionAsync();
            var hoy = DateTime.Today;
            var diaActual = hoy.Day;
            var mesActual = $"{hoy.Year}-{hoy.Month:D2}";
            
            System.Console.WriteLine($"[LICENCIA] Hoy: {hoy:dd/MM/yyyy} | Día: {diaActual} | Mes: {mesActual}");
            System.Console.WriteLine($"[LICENCIA] Licencia pagada: {config.LicenciaPagada} | Hasta: '{config.LicenciaPagadaHasta ?? "NULL"}'");
            
            // ✅ 4. Lógica de licencia
            bool licenciaAlDia = config.LicenciaPagada && config.LicenciaPagadaHasta == mesActual;
            
            if (!licenciaAlDia)
            {
                System.Console.WriteLine($"[LICENCIA] ⚠️ Licencia NO está al día");
                
                if (claveMaestraValida)
                {
                    // 🔑 CLAVE MAESTRA USADA → MARCAR LICENCIA COMO PAGADA AUTOMÁTICAMENTE
                    System.Console.WriteLine($"[LICENCIA] 🔑 Clave maestra válida - Actualizando BD...");
                    
                    try
                    {
                        // Actualizar configuración en Supabase
                        config.LicenciaPagada = true;
                        config.LicenciaPagadaHasta = mesActual;
                        config.NotaLicencia = $"Pagó con clave maestra el {hoy:dd/MM/yyyy}";
                        
                        await _supabase.ActualizarConfiguracionAsync(config);
                        
                        System.Console.WriteLine($"[LICENCIA] ✅ BD actualizada: licencia_pagada=true, hasta={mesActual}");
                        Message = "🔓 Acceso habilitado - Licencia marcada como pagada para este mes";
                    }
                    catch (System.Exception ex)
                    {
                        System.Console.WriteLine($"[LICENCIA] ❌ Error al actualizar BD: {ex.Message}");
                        Message = "🔓 Acceso habilitado (pero no se pudo actualizar la licencia)";
                    }
                }
                else if (diaActual >= 1 && diaActual <= 10)
                {
                    // 📅 Días 1-10: Recordatorio con TempData
                    TempData["LicenseWarning"] = $"⚠️ Recordatorio: Tu licencia vence el 10 de {hoy:MMMM}. Te quedan {10 - diaActual} días para regularizar.";
                    System.Console.WriteLine($"[LICENCIA] ✅ TempData['LicenseWarning'] establecido");
                }
                else if (diaActual > 10)
                {
                    // 📅 Día 11+: BLOQUEO
                    BloqueadoPorLicencia = true;
                    var diasVencido = diaActual - 10;
                    MensajeBloqueo = $"❌ Licencia Vencida\n\nEl pago debía realizarse entre el 1° y 10° de {hoy:MMMM}.\n\n📅 Hoy es {hoy:dd 'de' MMMM} - Tu acceso está bloqueado hace {diasVencido} días.\n\n💬 Contactá al desarrollador para regularizar.";
                    System.Console.WriteLine($"[LICENCIA] ❌ BLOQUEADO");
                    return Page();
                }
            }
            else
            {
                System.Console.WriteLine($"[LICENCIA] ✅ Licencia al día");
            }
            
            // ✅ 5. Login exitoso
            HttpContext.Session.SetString("AdminLogged", "true");
            
            System.Console.WriteLine($"[LOGIN] ✅ Redirigiendo a Productos");
            return RedirectToPage("/Admin/Productos");
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using CarniceriaWhatsApp.Services;
using CarniceriaWhatsApp.Models;

namespace CarniceriaWhatsApp.Pages.Admin
{
    public class LoginModel : PageModel
    {
        private readonly ISupabaseService _supabase;
        
        // 🔑 CLAVE MAESTRA (CAMBIAR por una propia más segura)
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
            System.Console.WriteLine("========================================");
            System.Console.WriteLine($"[LOGIN] Intento: {Username} | Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}");
            
            // ✅ 1. Verificar credenciales
            if (Username != "admin" || Password != "admin123")
            {
                Message = "❌ Usuario o contraseña incorrectos";
                return Page();
            }
            
            // ✅ 2. Verificar CLAVE MAESTRA
            bool claveMaestraValida = !string.IsNullOrEmpty(MasterKey) && MasterKey == MASTER_KEY;
            
            // ✅ 3. Obtener configuración
            var config = await _supabase.ObtenerConfiguracionAsync();
            var hoy = DateTime.Today;
            var diaActual = hoy.Day;
            var mesActual = $"{hoy.Year}-{hoy.Month:D2}";
            
            System.Console.WriteLine($"[LICENCIA] Día: {diaActual} | Mes: {mesActual}");
            System.Console.WriteLine($"[LICENCIA] Pagada: {config.LicenciaPagada} | Hasta: '{config.LicenciaPagadaHasta ?? "NULL"}'");
            
            // ✅ 4. Verificar si licencia está al día
            bool licenciaAlDia = config.LicenciaPagada && config.LicenciaPagadaHasta == mesActual;
            
            if (!licenciaAlDia)
            {
                System.Console.WriteLine($"[LICENCIA] ⚠️ NO está al día");
                
                // 🔑 CLAVE MAESTRA → ACTUALIZAR BD PERMANENTEMENTE
                if (claveMaestraValida)
                {
                    System.Console.WriteLine($"[LICENCIA] 🔑 Clave maestra válida - ACTIVANDO licencia...");
                    
                    try
                    {
                        // Actualizar configuración en Supabase
                        config.LicenciaPagada = true;
                        config.LicenciaPagadaHasta = mesActual;
                        config.NotaLicencia = $"Activado con clave maestra el {hoy:dd/MM/yyyy}";
                        
                        await _supabase.ActualizarConfiguracionAsync(config);
                        
                        System.Console.WriteLine($"[LICENCIA] ✅ BD ACTUALIZADA: licencia_pagada=true, hasta={mesActual}");
                        Message = "🔓 Acceso habilitado - Licencia activada para este mes";
                    }
                    catch (System.Exception ex)
                    {
                        System.Console.WriteLine($"[LICENCIA] ❌ Error: {ex.Message}");
                        Message = "🔓 Acceso habilitado (error al actualizar BD)";
                    }
                }
                // 📅 Días 1-10: Banner amarillo (permite entrar)
                else if (diaActual >= 1 && diaActual <= 10)
                {
                    var diasRestantes = 10 - diaActual;
                    TempData["LicenseWarning"] = $"⚠️ Recordatorio: Tu licencia vence el 10 de {hoy:MMMM}. Te quedan {diasRestantes} días para regularizar.";
                    System.Console.WriteLine($"[LICENCIA] 📅 Días 1-10: Banner amarillo mostrado");
                }
                // 📅 Día 11+: BLOQUEO TOTAL
                else if (diaActual > 10)
                {
                    BloqueadoPorLicencia = true;
                    var diasVencido = diaActual - 10;
                    MensajeBloqueo = $"❌ LICENCIA VENCIDA\n\nEl pago debía realizarse entre el 1° y 10° de {hoy:MMMM}.\n\n📅 Hoy es {hoy:dd 'de' MMMM} - Tu acceso está BLOQUEADO.\n\n💬 Contactá al desarrollador para regularizar.";
                    System.Console.WriteLine($"[LICENCIA] ❌ DÍA {diaActual}: BLOQUEADO");
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
            System.Console.WriteLine("========================================");
            
            return RedirectToPage("/Admin/Productos");
        }
    }
}

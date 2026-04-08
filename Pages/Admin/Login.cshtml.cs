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
        private readonly HttpClient _httpClient;
        
        private const string MASTER_KEY = "DEV_MASTER_KEY_2026_CARNICERIA_DV7x9Kp2";
        
        public string Message { get; set; } = "";
        public bool BloqueadoPorLicencia { get; set; } = false;
        public string MensajeBloqueo { get; set; } = "";
        
        public LoginModel(ISupabaseService supabase, HttpClient httpClient)
        {
            _supabase = supabase;
            _httpClient = httpClient;
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
                    // 🔑 CLAVE MAESTRA USADA → ACTUALIZAR BD DIRECTAMENTE CON HTTP REQUEST
                    System.Console.WriteLine($"[LICENCIA] 🔑 Clave maestra válida - Actualizando BD directamente...");
                    
                    try
                    {
                        // Obtener URL y key de Supabase desde variables de entorno
                        var supabaseUrl = Environment.GetEnvironmentVariable("Supabase__Url") 
                            ?? "https://wfmoyssyoyqnxqjqedcc.supabase.co";
                        var supabaseKey = Environment.GetEnvironmentVariable("Supabase__ApiKey");
                        
                        if (string.IsNullOrEmpty(supabaseKey))
                        {
                            System.Console.WriteLine($"[LICENCIA] ❌ ERROR: No hay Supabase API Key configurada");
                            Message = "🔓 Acceso habilitado (pero no se pudo actualizar la licencia - sin API key)";
                        }
                        else
                        {
                            // Actualizar directamente vía REST API
                            var updateData = new
                            {
                                licencia_pagada = true,
                                licencia_pagada_hasta = mesActual,
                                nota_licencia = $"Pagó con clave maestra el {hoy:dd/MM/yyyy}",
                                actualizado_en = DateTime.UtcNow.ToString("o")
                            };
                            
                            var json = JsonSerializer.Serialize(updateData);
                            var content = new StringContent(json, Encoding.UTF8, "application/json");
                            
                            // Buscar el ID de la configuración (generalmente es 1)
                            var configId = config.Id ?? 1;
                            
                            using (var client = new HttpClient())
                            {
                                client.DefaultRequestHeaders.Add("apikey", supabaseKey);
                                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
                                client.DefaultRequestHeaders.Add("Content-Type", "application/json");
                                client.DefaultRequestHeaders.Add("Prefer", "return=representation");
                                
                                var url = $"{supabaseUrl}/rest/v1/configuracion_carniceria?id=eq.{configId}";
                                var response = await client.PatchAsync(url, content);
                                
                                if (response.IsSuccessStatusCode)
                                {
                                    System.Console.WriteLine($"[LICENCIA] ✅ BD actualizada EXITOSAMENTE: licencia_pagada=true, hasta={mesActual}");
                                    Message = "🔓 Acceso habilitado - Licencia marcada como pagada para este mes";
                                }
                                else
                                {
                                    var errorBody = await response.Content.ReadAsStringAsync();
                                    System.Console.WriteLine($"[LICENCIA] ❌ Error HTTP {response.StatusCode}: {errorBody}");
                                    Message = $"🔓 Acceso habilitado (pero error al actualizar: {response.StatusCode})";
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        System.Console.WriteLine($"[LICENCIA] ❌ Exception al actualizar BD: {ex.Message}");
                        System.Console.WriteLine($"[LICENCIA] Stack: {ex.StackTrace}");
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

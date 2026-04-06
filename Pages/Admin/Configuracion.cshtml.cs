using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using CarniceriaWhatsApp.Models;
using CarniceriaWhatsApp.Services;

namespace CarniceriaWhatsApp.Pages.Admin
{
    public class ConfiguracionModel : PageModel
    {
        private readonly ISupabaseService _supabase;
        
        public ConfiguracionModel(ISupabaseService supabase)
        {
            _supabase = supabase;
        }
        
        [BindProperty]
        public ConfiguracionCarniceria? Config { get; set; }
        
        public string Message { get; set; } = "";
        public bool IsError { get; set; }
        
        public async Task<IActionResult> OnGetAsync()
        {
            Config = await _supabase.ObtenerConfiguracionAsync();
            return Page();
        }
        
        public async Task<IActionResult> OnPostAsync()
        {
            // Validar que tenemos una configuración válida
            if (Config == null)
            {
                Message = "❌ Datos inválidos";
                IsError = true;
                return Page();
            }
            
            // Si el Id es 0 o null, buscamos el primer registro existente
            if (Config.Id == null || Config.Id == 0)
            {
                var existente = await _supabase.ObtenerConfiguracionAsync();
                if (existente != null && existente.Id != null)
                {
                    Config.Id = existente.Id;
                }
            }
            
            try
            {
                // Actualizar en Supabase
                var actualizado = await _supabase.ActualizarConfiguracionAsync(Config);
                
                if (actualizado != null)
                {
                    Message = "✅ Configuración guardada correctamente";
                    IsError = false;
                    // Recargar la configuración actualizada
                    Config = await _supabase.ObtenerConfiguracionAsync();
                    return Page();
                }
                else
                {
                    Message = "⚠️ No se pudo confirmar la actualización";
                    IsError = true;
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[CONFIG ERROR] {ex.Message}");
                Message = "❌ Error: " + ex.Message;
                IsError = true;
            }
            
            // Si llegamos acá, mantener los datos del formulario
            return Page();
        }
    }
}

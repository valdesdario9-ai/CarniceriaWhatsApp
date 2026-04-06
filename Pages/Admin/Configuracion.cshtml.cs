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
            if (Config == null)
            {
                Message = "❌ Datos inválidos";
                IsError = true;
                return Page();
            }
            
            if (Config.Id == null || Config.Id == 0)
            {
                var existente = await _supabase.ObtenerConfiguracionAsync();
                if (existente != null && existente.Id != null)
                {
                    Config.Id = existente.Id;
                    System.Console.WriteLine($"[CONFIG] Usando ID existente: {Config.Id}");
                }
            }
            
            try
            {
                var actualizado = await _supabase.ActualizarConfiguracionAsync(Config);
                
                if (actualizado != null)
                {
                    Message = "✅ Configuración guardada correctamente";
                    IsError = false;
                    Config = await _supabase.ObtenerConfiguracionAsync();
                    System.Console.WriteLine($"[CONFIG] Guardado exitosamente");
                    return Page();
                }
                else
                {
                    Message = "⚠️ No se pudo confirmar la actualización";
                    IsError = true;
                    System.Console.WriteLine($"[CONFIG] Actualización retornó null");
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[CONFIG ERROR] {ex.Message}");
                Message = "❌ Error: " + ex.Message;
                IsError = true;
            }
            
            return Page();
        }
    }
}

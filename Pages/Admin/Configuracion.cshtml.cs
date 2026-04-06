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
        public ConfiguracionCarniceria Config { get; set; }
        
        public string Message { get; set; } = "";
        public bool IsError { get; set; }
        
        public async Task<IActionResult> OnGetAsync()
        {
            Config = await _supabase.ObtenerConfiguracionAsync();
            return Page();
        }
        
        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Config.NombreTienda))
            {
                Message = "❌ El nombre es obligatorio";
                IsError = true;
                return Page();
            }
            
            try
            {
                await _supabase.ActualizarConfiguracionAsync(Config);
                Message = "✅ Configuración guardada correctamente";
                IsError = false;
                await Task.Delay(300);
                return RedirectToPage("Configuracion");
            }
            catch (Exception ex)
            {
                Message = "❌ Error: " + ex.Message;
                IsError = true;
                return Page();
            }
        }
    }
}

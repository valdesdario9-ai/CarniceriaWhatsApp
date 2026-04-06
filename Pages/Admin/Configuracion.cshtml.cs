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
        public ConfiguracionModel(ISupabaseService supabase) => _supabase = supabase;
        
        [BindProperty] public ConfiguracionCarniceria Config { get; set; }
        public string Message { get; set; } = "";
        
        public async Task<IActionResult> OnGetAsync()
        {
            Config = await _supabase.ObtenerConfiguracionAsync();
            return Page();
        }
        
        public async Task<IActionResult> OnPostAsync()
        {
            await _supabase.ActualizarConfiguracionAsync(Config);
            Message = "✅ Configuración guardada correctamente";
            Config = await _supabase.ObtenerConfiguracionAsync();
            return Page();
        }
    }
}

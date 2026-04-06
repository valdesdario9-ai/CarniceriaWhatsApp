using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using CarniceriaWhatsApp.Models;
using CarniceriaWhatsApp.Services;

namespace CarniceriaWhatsApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ISupabaseService _supabase;
        
        public IndexModel(ISupabaseService supabase)
        {
            _supabase = supabase;
        }
        
        public List<Producto> Productos { get; set; } = new List<Producto>();
        public ConfiguracionCarniceria Config { get; set; }
        
        public async Task OnGetAsync()
        {
            Productos = await _supabase.ObtenerProductosAsync();
            Config = await _supabase.ObtenerConfiguracionAsync();
        }
    }
}

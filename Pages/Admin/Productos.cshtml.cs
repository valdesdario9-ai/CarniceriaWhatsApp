using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;
using CarniceriaWhatsApp.Models;
using CarniceriaWhatsApp.Services;

namespace CarniceriaWhatsApp.Pages.Admin
{
    public class ProductosModel : PageModel
    {
        private readonly ISupabaseService _supabase;
        
        public ProductosModel(ISupabaseService supabase)
        {
            _supabase = supabase;
        }
        
        public List<Producto> Productos { get; set; } = new List<Producto>();
        
        public async Task<IActionResult> OnGetAsync()
        {
            Productos = await _supabase.ObtenerProductosAsync();
            return Page();
        }
        
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            await _supabase.EliminarProductoAsync(id);
            return RedirectToPage();
        }
    }
}

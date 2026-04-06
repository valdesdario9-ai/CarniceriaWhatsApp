using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using CarniceriaWhatsApp.Models;
using CarniceriaWhatsApp.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace CarniceriaWhatsApp.Pages.Admin
{
    public class EditarModel : PageModel
    {
        private readonly ISupabaseService _supabase;
        public EditarModel(ISupabaseService supabase) => _supabase = supabase;
        
        [BindProperty] public Producto Producto { get; set; }
        [BindProperty] public IFormFile ImagenFile { get; set; }
        public string Message { get; set; } = "";
        public bool IsError { get; set; }
        
        public async Task<IActionResult> OnGetAsync(int id)
        {
            Producto = await _supabase.ObtenerProductoAsync(id);
            if (Producto == null) return Page();
            return Page();
        }
        
        public async Task<IActionResult> OnPostAsync()
        {
            if (Producto == null || string.IsNullOrEmpty(Producto.Nombre))
            {
                Message = "❌ Datos inválidos";
                IsError = true;
                return Page();
            }
            
            if (ImagenFile != null && ImagenFile.Length > 0)
            {
                using (var stream = ImagenFile.OpenReadStream())
                {
                    var ext = Path.GetExtension(ImagenFile.FileName).ToLower();
                    var fileName = $"prod_{Guid.NewGuid()}{ext}";
                    Producto.ImagenUrl = await _supabase.SubirImagenAsync(stream, fileName, ImagenFile.ContentType);
                }
            }
            
            await _supabase.ActualizarProductoAsync(Producto.Id, Producto);
            return RedirectToPage("Productos");
        }
    }
}

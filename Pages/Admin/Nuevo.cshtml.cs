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
    public class NuevoModel : PageModel
    {
        private readonly ISupabaseService _supabase;
        public NuevoModel(ISupabaseService supabase) => _supabase = supabase;
        
        [BindProperty] public string Nombre { get; set; } = "";
        [BindProperty] public decimal PrecioPorKilo { get; set; }
        [BindProperty] public IFormFile ImagenFile { get; set; }
        [BindProperty] public bool Activo { get; set; } = true;
        [BindProperty] public string Categoria { get; set; } = "";
        
        public string Message { get; set; } = "";
        public bool IsError { get; set; }
        
        public IActionResult OnGet() => Page();
        
        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Nombre) || PrecioPorKilo <= 0)
            {
                Message = "❌ Nombre y precio son obligatorios";
                IsError = true;
                return Page();
            }
            
            string imagenUrl = "https://via.placeholder.com/300";
            if (ImagenFile != null && ImagenFile.Length > 0)
            {
                using (var stream = ImagenFile.OpenReadStream())
                {
                    var ext = Path.GetExtension(ImagenFile.FileName).ToLower();
                    var fileName = $"prod_{Guid.NewGuid()}{ext}";
                    imagenUrl = await _supabase.SubirImagenAsync(stream, fileName, ImagenFile.ContentType);
                }
            }
            
            var producto = new Producto {
                Nombre = Nombre,
                PrecioPorKilo = PrecioPorKilo,
                ImagenUrl = imagenUrl,
                Activo = Activo,
                Categoria = Categoria
            };
            
            await _supabase.CrearProductoAsync(producto);
            return RedirectToPage("Productos");
        }
    }
}

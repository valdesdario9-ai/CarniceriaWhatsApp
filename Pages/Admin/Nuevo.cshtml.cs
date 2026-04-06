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
        
        public NuevoModel(ISupabaseService supabase)
        {
            _supabase = supabase;
        }
        
        [BindProperty]
        public string Nombre { get; set; } = "";
        
        [BindProperty]
        public decimal PrecioPorKilo { get; set; }
        
        [BindProperty]
        public IFormFile ImagenFile { get; set; }
        
        [BindProperty]
        public bool Activo { get; set; } = true;
        
        [BindProperty]
        public string Categoria { get; set; } = "";
        
        public string Message { get; set; } = "";
        public bool IsError { get; set; }
        
        public IActionResult OnGet()
        {
            return Page();
        }
        
        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Nombre))
            {
                Message = "❌ El nombre es obligatorio";
                IsError = true;
                return Page();
            }
            
            if (PrecioPorKilo <= 0)
            {
                Message = "❌ El precio debe ser mayor a 0";
                IsError = true;
                return Page();
            }
            
            string imagenUrl = "https://via.placeholder.com/300";
            
            if (ImagenFile != null && ImagenFile.Length > 0)
            {
                try
                {
                    using (var stream = ImagenFile.OpenReadStream())
                    {
                        var extension = Path.GetExtension(ImagenFile.FileName).ToLower();
                        var fileName = $"producto_{Guid.NewGuid()}{extension}";
                        imagenUrl = await _supabase.SubirImagenAsync(stream, fileName, ImagenFile.ContentType);
                    }
                }
                catch (Exception ex)
                {
                    Message = "⚠️ Producto guardado pero imagen falló: " + ex.Message;
                    IsError = true;
                }
            }
            
            var producto = new Producto
            {
                Nombre = Nombre,
                PrecioPorKilo = PrecioPorKilo,
                ImagenUrl = imagenUrl,
                Activo = Activo,
                Categoria = Categoria
            };
            
            try
            {
                await _supabase.CrearProductoAsync(producto);
                Message = "✅ Producto guardado correctamente";
                IsError = false;
                await Task.Delay(500);
                return RedirectToPage("Productos");
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

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
        
        public EditarModel(ISupabaseService supabase)
        {
            _supabase = supabase;
        }
        
        [BindProperty]
        public Producto? Producto { get; set; }
        
        [BindProperty]
        public IFormFile? ImagenFile { get; set; }
        
        public string Message { get; set; } = "";
        public bool IsError { get; set; }
        
        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0)
                return RedirectToPage("Productos");
            
            Producto = await _supabase.ObtenerProductoAsync(id);
            
            if (Producto == null)
            {
                Message = "❌ Producto no encontrado";
                IsError = true;
                return Page();
            }
            
            return Page();
        }
        
        public async Task<IActionResult> OnPostAsync()
        {
            // Validar que tenemos un producto válido
            if (Producto == null || Producto.Id <= 0)
            {
                Message = "❌ ID de producto inválido";
                IsError = true;
                return Page();
            }
            
            // Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(Producto.Nombre))
            {
                Message = "❌ El nombre es obligatorio";
                IsError = true;
                return Page();
            }
            
            if (Producto.PrecioPorKilo <= 0)
            {
                Message = "❌ El precio debe ser mayor a 0";
                IsError = true;
                return Page();
            }
            
            // Si se subió una nueva imagen, procesarla
            if (ImagenFile != null && ImagenFile.Length > 0)
            {
                try
                {
                    using (var stream = ImagenFile.OpenReadStream())
                    {
                        var extension = Path.GetExtension(ImagenFile.FileName).ToLower();
                        var fileName = $"prod_{Guid.NewGuid()}{extension}";
                        Producto.ImagenUrl = await _supabase.SubirImagenAsync(stream, fileName, ImagenFile.ContentType);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR IMAGEN] {ex.Message}");
                    // Continuar sin actualizar la imagen si falla
                }
            }
            
            // Actualizar el producto en la base de datos
            try
            {
                var actualizado = await _supabase.ActualizarProductoAsync(Producto.Id, Producto);
                
                if (actualizado != null)
                {
                    Message = "✅ Producto actualizado correctamente";
                    IsError = false;
                    // Esperar un poco para asegurar que la BD se actualizó
                    await Task.Delay(100);
                    return RedirectToPage("Productos");
                }
                else
                {
                    Message = "⚠️ No se pudo confirmar la actualización";
                    IsError = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR UPDATE] {ex.Message}");
                Message = "❌ Error al actualizar: " + ex.Message;
                IsError = true;
            }
            
            // Si llegamos acá, recargar el producto para mostrar los datos actuales
            Producto = await _supabase.ObtenerProductoAsync(Producto.Id);
            return Page();
        }
    }
}

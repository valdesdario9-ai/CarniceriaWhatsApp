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
            }
            
            return Page();
        }
        
        public async Task<IActionResult> OnPostAsync()
        {
            if (Producto == null || Producto.Id <= 0)
            {
                Message = "❌ ID de producto inválido";
                IsError = true;
                return Page();
            }
            
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
                    System.Console.WriteLine($"[IMAGEN] Procesando archivo: {ImagenFile.FileName} ({ImagenFile.Length} bytes)");
                    
                    using (var stream = ImagenFile.OpenReadStream())
                    {
                        var extension = Path.GetExtension(ImagenFile.FileName).ToLower();
                        var fileName = $"prod_{Guid.NewGuid()}{extension}";
                        
                        System.Console.WriteLine($"[IMAGEN] Subiendo: {fileName}");
                        
                        Producto.ImagenUrl = await _supabase.SubirImagenAsync(stream, fileName, ImagenFile.ContentType);
                        
                        System.Console.WriteLine($"[IMAGEN] URL resultante: {Producto.ImagenUrl}");
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[IMAGEN ERROR] {ex.Message}");
                    Message = "⚠️ Producto actualizado pero imagen falló: " + ex.Message;
                    IsError = true;
                }
            }
            
            try
            {
                System.Console.WriteLine($"[UPDATE] Actualizando producto {Producto.Id}");
                
                var actualizado = await _supabase.ActualizarProductoAsync(Producto.Id, Producto);
                
                if (actualizado != null)
                {
                    Message = "✅ Producto actualizado correctamente";
                    IsError = false;
                    System.Console.WriteLine($"[UPDATE] Éxito");
                    await Task.Delay(100);
                    return RedirectToPage("Productos");
                }
                else
                {
                    Message = "⚠️ No se pudo confirmar la actualización";
                    IsError = true;
                    System.Console.WriteLine($"[UPDATE] Retornó null");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[UPDATE ERROR] {ex.Message}");
                Message = "❌ Error al actualizar: " + ex.Message;
                IsError = true;
            }
            
            Producto = await _supabase.ObtenerProductoAsync(Producto.Id);
            return Page();
        }
    }
}

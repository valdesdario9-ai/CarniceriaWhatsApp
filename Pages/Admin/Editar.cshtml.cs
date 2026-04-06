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
        public Producto Producto { get; set; }
        
        [BindProperty]
        public IFormFile ImagenFile { get; set; }
        
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
            
            if (string.IsNullOrEmpty(Producto.Nombre))
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
            
            if (ImagenFile != null && ImagenFile.Length > 0)
            {
                try
                {
                    using (var stream = ImagenFile.OpenReadStream())
                    {
                        var extension = Path.GetExtension(ImagenFile.FileName).ToLower();
                        var fileName = $"producto_{Guid.NewGuid()}{extension}";
                        Producto.ImagenUrl = await _supabase.SubirImagenAsync(stream, fileName, ImagenFile.ContentType);
                    }
                }
                catch (Exception ex)
                {
                    Message = "⚠️ Producto actualizado pero imagen falló: " + ex.Message;
                    IsError = true;
                }
            }
            
            try
            {
                await _supabase.ActualizarProductoAsync(Producto.Id, Producto);
                Message = "✅ Producto actualizado correctamente";
                IsError = false;
                await Task.Delay(300);
                return RedirectToPage("Productos");
            }
            catch (Exception ex)
            {
                Message = "❌ Error al actualizar: " + ex.Message;
                IsError = true;
                return Page();
            }
        }
    }
}

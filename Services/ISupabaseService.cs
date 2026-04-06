using System.Collections.Generic;
using System.Threading.Tasks;
using CarniceriaWhatsApp.Models;
using System.IO;

namespace CarniceriaWhatsApp.Services
{
    public interface ISupabaseService
    {
        Task<List<Producto>> ObtenerProductosAsync();
        Task<Producto> ObtenerProductoAsync(int id);
        Task<Producto> CrearProductoAsync(Producto producto);
        Task<Producto> ActualizarProductoAsync(int id, Producto producto);
        Task<bool> EliminarProductoAsync(int id);
        Task<string> SubirImagenAsync(Stream imagenStream, string fileName, string contentType);
        
        Task<ConfiguracionCarniceria> ObtenerConfiguracionAsync();
        Task<ConfiguracionCarniceria> ActualizarConfiguracionAsync(ConfiguracionCarniceria config);
    }
}

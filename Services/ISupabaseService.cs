using System.Collections.Generic;
using System.Threading.Tasks;
using CarniceriaWhatsApp.Models;

namespace CarniceriaWhatsApp.Services
{
    public interface ISupabaseService
    {
        // Productos
        Task<List<Producto>> ObtenerProductosAsync();
        Task<Producto?> ObtenerProductoAsync(int id);
        Task<Producto> CrearProductoAsync(Producto producto);
        Task<Producto?> ActualizarProductoAsync(int id, Producto producto);
        Task<bool> EliminarProductoAsync(int id);
        Task<string> SubirImagenAsync(System.IO.Stream imagenStream, string fileName, string contentType);
        
        // Configuración
        Task<ConfiguracionCarniceria> ObtenerConfiguracionAsync();
        Task<ConfiguracionCarniceria> ActualizarConfiguracionAsync(ConfiguracionCarniceria config);
        
        // ✅ NUEVOS: Pedidos y Reportes
        Task<Pedido> CrearPedidoAsync(Pedido pedido);
        Task<List<Pedido>> ObtenerPedidosAsync(int dias);
        Task<List<Pedido>> ObtenerPedidosPorEstadoAsync(string estado);
        Task<ReporteVentas> ObtenerReporteVentasAsync(int dias);
        Task<bool> ActualizarEstadoPedidoAsync(long pedidoId, string estado);
    }
}

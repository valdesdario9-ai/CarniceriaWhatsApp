using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CarniceriaWhatsApp.Models
{
    public class Pedido
    {
        public long? Id { get; set; }
        public string NombreCliente { get; set; } = "";
        public string TelefonoCliente { get; set; } = "";
        public string DireccionCliente { get; set; } = "";
        public string NotasCliente { get; set; } = "";
        public decimal Total { get; set; }
        public string Estado { get; set; } = "pendiente";
        
        // ✅ Mapear created_at de Supabase
        [JsonPropertyName("created_at")]
        public DateTime? CreadoEn { get; set; }
        
        public List<PedidoDetalle> Detalles { get; set; } = new List<PedidoDetalle>();
    }

    public class PedidoDetalle
    {
        public long? Id { get; set; }
        
        // ✅ CRÍTICO: Mapear columnas snake_case de Supabase
        [JsonPropertyName("pedido_id")]
        public long PedidoId { get; set; }
        
        [JsonPropertyName("producto_id")]
        public long ProductoId { get; set; }
        
        [JsonPropertyName("producto_nombre")]
        public string ProductoNombre { get; set; } = "";
        
        [JsonPropertyName("precio_por_kilo")]
        public decimal PrecioPorKilo { get; set; }
        
        [JsonPropertyName("cantidad")]
        public decimal Cantidad { get; set; }
        
        [JsonPropertyName("subtotal")]
        public decimal Subtotal { get; set; }
        
        [JsonPropertyName("created_at")]
        public DateTime? CreadoEn { get; set; }
    }

    public class ReporteVentas
    {
        public decimal TotalVentas { get; set; }
        public int TotalPedidos { get; set; }
        public decimal TicketPromedio { get; set; }
        public List<VentaPorDia> VentasPorDia { get; set; } = new List<VentaPorDia>();
        public List<ProductoMasVendido> ProductosMasVendidos { get; set; } = new List<ProductoMasVendido>();
        public List<VentaPorEstado> VentasPorEstado { get; set; } = new List<VentaPorEstado>();
    }

    public class VentaPorDia
    {
        public string Fecha { get; set; } = "";
        public decimal Total { get; set; }
        public int Pedidos { get; set; }
    }

    public class ProductoMasVendido
    {
        public string Nombre { get; set; } = "";
        public decimal CantidadVendida { get; set; }
        public decimal TotalVendido { get; set; }
        public int VecesVendido { get; set; }
    }

    public class VentaPorEstado
    {
        public string Estado { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal Total { get; set; }
    }
}

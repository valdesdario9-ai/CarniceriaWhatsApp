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
        
        // ✅ CRÍTICO: Mapear created_at de Supabase a CreadoEn en C#
        [JsonPropertyName("created_at")]
        public DateTime? CreadoEn { get; set; }
        
        public List<PedidoDetalle> Detalles { get; set; } = new List<PedidoDetalle>();
    }

    public class PedidoDetalle
    {
        public long? Id { get; set; }
        public long PedidoId { get; set; }
        public long ProductoId { get; set; }
        public string ProductoNombre { get; set; } = "";
        public decimal PrecioPorKilo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal Subtotal { get; set; }
        
        // ✅ También mapear created_at si existe
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

using System;
using System.Text.Json.Serialization;

namespace CarniceriaWhatsApp.Models
{
    public class ConfiguracionCarniceria
    {
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long? Id { get; set; }
        
        [JsonPropertyName("nombre_tienda")]
        public string NombreTienda { get; set; } = "Mi Carnicería";
        
        [JsonPropertyName("descripcion")]
        public string Descripcion { get; set; } = "";
        
        [JsonPropertyName("telefono")]
        public string Telefono { get; set; } = "";
        
        [JsonPropertyName("whatsapp")]
        public string Whatsapp { get; set; } = "";
        
        [JsonPropertyName("email")]
        public string Email { get; set; } = "";
        
        [JsonPropertyName("direccion")]
        public string Direccion { get; set; } = "";
        
        [JsonPropertyName("ciudad")]
        public string Ciudad { get; set; } = "";
        
        [JsonPropertyName("provincia")]
        public string Provincia { get; set; } = "";
        
        [JsonPropertyName("logo_url")]
        public string LogoUrl { get; set; } = "https://via.placeholder.com/150";
        
        [JsonPropertyName("banner_url")]
        public string BannerUrl { get; set; } = "https://via.placeholder.com/1200x300";
        
        [JsonPropertyName("horarios")]
        public string Horarios { get; set; } = "Lunes a Sábado: 8:00 - 20:00";
        
        [JsonPropertyName("alias_mercadopago")]
        public string AliasMercadoPago { get; set; } = "";
        
        [JsonPropertyName("cbu")]
        public string Cbu { get; set; } = "";
        
        [JsonPropertyName("instrucciones_pago")]
        public string InstruccionesPago { get; set; } = "Una vez confirmado el pago, preparamos tu pedido.";
        
        [JsonPropertyName("creado_en")]
        public DateTime? CreadoEn { get; set; }
        
        [JsonPropertyName("actualizado_en")]
        public DateTime? ActualizadoEn { get; set; }
        
        // ✅ PROPIEDADES DE LICENCIA (CON MAPEO JSON EXPLÍCITO)
        [JsonPropertyName("licencia_pagada")]
        public bool LicenciaPagada { get; set; } = false;
        
        [JsonPropertyName("licencia_pagada_hasta")]
        public string? LicenciaPagadaHasta { get; set; }
        
        [JsonPropertyName("nota_licencia")]
        public string? NotaLicencia { get; set; } = "";
    }
}

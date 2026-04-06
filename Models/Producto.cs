using System.Text.Json.Serialization;

namespace CarniceriaWhatsApp.Models
{
    public class Producto
    {
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int Id { get; set; }
        
        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = "";
        
        [JsonPropertyName("precio_por_kilo")]
        public decimal PrecioPorKilo { get; set; }
        
        [JsonPropertyName("imagen_url")]
        public string ImagenUrl { get; set; } = "https://via.placeholder.com/300";
        
        [JsonPropertyName("categoria")]
        public string Categoria { get; set; } = "";
        
        [JsonPropertyName("activo")]
        public bool Activo { get; set; } = true;
    }
}

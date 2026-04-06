using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CarniceriaWhatsApp.Models;
using System.IO;
using System.Text.RegularExpressions;

namespace CarniceriaWhatsApp.Services
{
    public class SupabaseService : ISupabaseService
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _supabaseKey;
        
        public SupabaseService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _supabaseUrl = Environment.GetEnvironmentVariable("Supabase__Url") 
                ?? "https://wfmoyssyoyqnxqjqedcc.supabase.co";
            _supabaseKey = Environment.GetEnvironmentVariable("Supabase__ApiKey");
            
            if (!string.IsNullOrEmpty(_supabaseKey))
            {
                _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseKey}");
            }
            _httpClient.BaseAddress = new Uri(_supabaseUrl);
        }
        
        public async Task<List<Producto>> ObtenerProductosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/rest/v1/productos_carniceria?select=*&activo=eq.true");
                if (!response.IsSuccessStatusCode) return new List<Producto>();
                return await response.Content.ReadFromJsonAsync<List<Producto>>() ?? new List<Producto>();
            }
            catch { return new List<Producto>(); }
        }
        
        public async Task<Producto> ObtenerProductoAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/rest/v1/productos_carniceria?id=eq.{id}&select=*");
                if (!response.IsSuccessStatusCode) return null;
                var list = await response.Content.ReadFromJsonAsync<List<Producto>>();
                return list?.Count > 0 ? list[0] : null;
            }
            catch { return null; }
        }
        
        public async Task<Producto> CrearProductoAsync(Producto producto)
        {
            try
            {
                if (string.IsNullOrEmpty(producto.ImagenUrl))
                    producto.ImagenUrl = "https://via.placeholder.com/300";
                
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
                var json = JsonSerializer.Serialize(producto, options);
                
                if (producto.Id == 0)
                {
                    json = Regex.Replace(json, @"""id"":\s*0,?", "").Trim();
                    if (json.StartsWith("{,")) json = "{" + json.Substring(2);
                }
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/rest/v1/productos_carniceria?select=*", content);
                
                if (!response.IsSuccessStatusCode) return producto;
                
                var result = await response.Content.ReadFromJsonAsync<List<Producto>>();
                return result?.Count > 0 ? result[0] : producto;
            }
            catch { return producto; }
        }
        
        public async Task<Producto> ActualizarProductoAsync(int id, Producto producto)
        {
            try
            {
                var updateData = new
                {
                    nombre = producto.Nombre,
                    precio_por_kilo = producto.PrecioPorKilo,
                    imagen_url = producto.ImagenUrl,
                    categoria = producto.Categoria,
                    activo = producto.Activo
                };
                
                var options = new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                
                var json = JsonSerializer.Serialize(updateData, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var url = $"/rest/v1/productos_carniceria?id=eq.{id}&select=*";
                var response = await _httpClient.PatchAsync(url, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[UPDATE ERROR] {response.StatusCode}: {error}");
                    return null;
                }
                
                var result = await response.Content.ReadFromJsonAsync<List<Producto>>();
                return result?.Count > 0 ? result[0] : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UPDATE EXCEPTION] {ex.Message}");
                return null;
            }
        }
        
        public async Task<bool> EliminarProductoAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/rest/v1/productos_carniceria?id=eq.{id}");
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
        
        public async Task<string> SubirImagenAsync(Stream imagenStream, string fileName, string contentType)
        {
            try
            {
                if (string.IsNullOrEmpty(_supabaseKey))
                    return "https://via.placeholder.com/300";
                
                using (var ms = new MemoryStream())
                {
                    await imagenStream.CopyToAsync(ms);
                    var bytes = ms.ToArray();
                    
                    var uploadUrl = $"{_supabaseUrl}/storage/v1/object/productos-carniceria/{fileName}";
                    
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("apikey", _supabaseKey);
                        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseKey}");
                        
                        using (var content = new ByteArrayContent(bytes))
                        {
                            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                            var response = await client.PutAsync(uploadUrl, content);
                            
                            if (response.IsSuccessStatusCode)
                                return $"{_supabaseUrl}/storage/v1/object/public/productos-carniceria/{fileName}";
                            
                            return "https://via.placeholder.com/300";
                        }
                    }
                }
            }
            catch
            {
                return "https://via.placeholder.com/300";
            }
        }
        
        public async Task<ConfiguracionCarniceria> ObtenerConfiguracionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/rest/v1/configuracion_carniceria?select=*&order=id.asc&limit=1");
                if (!response.IsSuccessStatusCode) return new ConfiguracionCarniceria();
                
                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(content) || content.Trim() == "[]")
                    return new ConfiguracionCarniceria();
                
                using (var doc = JsonDocument.Parse(content))
                {
                    var root = doc.RootElement;
                    if (root.GetArrayLength() == 0) return new ConfiguracionCarniceria();
                    
                    var item = root[0];
                    var config = new ConfiguracionCarniceria();
                    
                    if (item.TryGetProperty("id", out var p) && p.ValueKind != JsonValueKind.Null)
                        config.Id = p.TryGetInt64(out var v) ? v : null;
                    
                    config.NombreTienda = GetStr(item, "nombre_tienda");
                    config.Descripcion = GetStr(item, "descripcion");
                    config.Telefono = GetStr(item, "telefono");
                    config.Whatsapp = GetStr(item, "whatsapp");
                    config.Email = GetStr(item, "email");
                    config.Direccion = GetStr(item, "direccion");
                    config.Ciudad = GetStr(item, "ciudad");
                    config.Provincia = GetStr(item, "provincia");
                    config.LogoUrl = GetStr(item, "logo_url");
                    config.BannerUrl = GetStr(item, "banner_url");
                    config.Horarios = GetStr(item, "horarios");
                    config.AliasMercadoPago = GetStr(item, "alias_mercadopago");
                    config.Cbu = GetStr(item, "cbu");
                    config.InstruccionesPago = GetStr(item, "instrucciones_pago");
                    
                    return config;
                }
            }
            catch { return new ConfiguracionCarniceria(); }
        }
        
        private string GetStr(JsonElement item, string name)
        {
            if (item.TryGetProperty(name, out var prop) && prop.ValueKind != JsonValueKind.Null)
                return prop.GetString() ?? "";
            return "";
        }
        
        public async Task<ConfiguracionCarniceria> ActualizarConfiguracionAsync(ConfiguracionCarniceria config)
        {
            try
            {
                var temp = new {
                    nombre_tienda = config.NombreTienda,
                    descripcion = config.Descripcion,
                    telefono = config.Telefono,
                    whatsapp = config.Whatsapp,
                    email = config.Email,
                    direccion = config.Direccion,
                    ciudad = config.Ciudad,
                    provincia = config.Provincia,
                    logo_url = config.LogoUrl,
                    banner_url = config.BannerUrl,
                    horarios = config.Horarios,
                    alias_mercadopago = config.AliasMercadoPago,
                    cbu = config.Cbu,
                    instrucciones_pago = config.InstruccionesPago
                };
                
                var json = JsonSerializer.Serialize(temp);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                if (config.Id != null && config.Id > 0)
                {
                    var url = $"/rest/v1/configuracion_carniceria?id=eq.{config.Id}&select=*";
                    var response = await _httpClient.PatchAsync(url, content);
                    if (!response.IsSuccessStatusCode) return config;
                    
                    var result = await response.Content.ReadFromJsonAsync<List<ConfiguracionCarniceria>>();
                    return result?.Count > 0 ? result[0] : config;
                }
                else
                {
                    var url = "/rest/v1/configuracion_carniceria?select=*";
                    var response = await _httpClient.PostAsync(url, content);
                    if (!response.IsSuccessStatusCode) return config;
                    
                    var result = await response.Content.ReadFromJsonAsync<List<ConfiguracionCarniceria>>();
                    return result?.Count > 0 ? result[0] : config;
                }
            }
            catch { return new ConfiguracionCarniceria(); }
        }
    }
}

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
                ?? "https://TU-PROYECTO.supabase.co";
            _supabaseKey = Environment.GetEnvironmentVariable("Supabase__ApiKey") 
                ?? "TU-API-KEY";
            
            _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseKey}");
            _httpClient.BaseAddress = new Uri(_supabaseUrl);
        }
        
        public async Task<List<Producto>> ObtenerProductosAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/rest/v1/productos_carniceria?select=*&activo=eq.true");
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return new List<Producto>();
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<Producto>>() ?? new List<Producto>();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[SUPABASE] ERROR: {ex.Message}");
                return new List<Producto>();
            }
        }
        
        public async Task<Producto> ObtenerProductoAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/rest/v1/productos_carniceria?id=eq.{id}&select=*");
                response.EnsureSuccessStatusCode();
                var productos = await response.Content.ReadFromJsonAsync<List<Producto>>();
                return productos?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[SUPABASE] ERROR: {ex.Message}");
                return null;
            }
        }
        
        public async Task<Producto> CrearProductoAsync(Producto producto)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
                var json = JsonSerializer.Serialize(producto, options);
                
                if (producto.Id == 0)
                {
                    json = Regex.Replace(json, @"""id"":\s*0,?", "").Trim();
                    if (json.StartsWith("{,")) json = "{" + json.Substring(2);
                }
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/rest/v1/productos_carniceria?select=*", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    System.Console.WriteLine($"[SUPABASE] ERROR: {error}");
                    return producto;
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "[]")
                    return producto;
                
                try
                {
                    var productos = await response.Content.ReadFromJsonAsync<List<Producto>>();
                    return productos?.FirstOrDefault() ?? producto;
                }
                catch { return producto; }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[SUPABASE] EXCEPTION: {ex.Message}");
                return producto;
            }
        }
        
        public async Task<Producto> ActualizarProductoAsync(int id, Producto producto)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
                var json = JsonSerializer.Serialize(producto, options);
                
                if (producto.Id == 0)
                {
                    json = Regex.Replace(json, @"""id"":\s*0,?", "").Trim();
                    if (json.StartsWith("{,")) json = "{" + json.Substring(2);
                }
                
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync($"/rest/v1/productos_carniceria?id=eq.{id}&select=*", content);
                
                if (!response.IsSuccessStatusCode)
                    return producto;
                
                var responseContent = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "[]")
                    return producto;
                
                try
                {
                    var productos = await response.Content.ReadFromJsonAsync<List<Producto>>();
                    return productos?.FirstOrDefault() ?? producto;
                }
                catch { return producto; }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[SUPABASE] ERROR: {ex.Message}");
                return producto;
            }
        }
        
        public async Task<bool> EliminarProductoAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/rest/v1/productos_carniceria?id=eq.{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[SUPABASE] ERROR: {ex.Message}");
                return false;
            }
        }
        
        public async Task<string> SubirImagenAsync(Stream imagenStream, string fileName, string contentType)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    await imagenStream.CopyToAsync(ms);
                    var bytes = ms.ToArray();
                    var url = $"{_supabaseUrl}/storage/v1/object/productos-carniceria/{fileName}";
                    
                    using (var content = new ByteArrayContent(bytes))
                    {
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
                        using (var client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Add("apikey", _supabaseKey);
                            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseKey}");
                            var response = await client.PutAsync(url, content);
                            
                            if (response.IsSuccessStatusCode)
                                return $"{_supabaseUrl}/storage/v1/object/public/productos-carniceria/{fileName}";
                            
                            var error = await response.Content.ReadAsStringAsync();
                            System.Console.WriteLine($"[STORAGE] Error: {error}");
                            throw new Exception($"Error subiendo imagen: {response.StatusCode}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[STORAGE] EXCEPTION: {ex.Message}");
                throw;
            }
        }
        
        public async Task<ConfiguracionCarniceria> ObtenerConfiguracionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/rest/v1/configuracion_carniceria?select=*&order=id.asc&limit=1");
                if (!response.IsSuccessStatusCode)
                    return new ConfiguracionCarniceria();
                
                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(content) || content.Trim() == "[]")
                    return new ConfiguracionCarniceria();
                
                using (var doc = JsonDocument.Parse(content))
                {
                    var root = doc.RootElement;
                    if (root.GetArrayLength() == 0)
                        return new ConfiguracionCarniceria();
                    
                    var item = root[0];
                    var config = new ConfiguracionCarniceria();
                    
                    if (item.TryGetProperty("id", out var p) && p.ValueKind != JsonValueKind.Null)
                        config.Id = p.TryGetInt64(out var v) ? v : null;
                    
                    config.NombreTienda = GetPropString(item, "nombre_tienda");
                    config.Descripcion = GetPropString(item, "descripcion");
                    config.Whatsapp = GetPropString(item, "whatsapp");
                    config.Telefono = GetPropString(item, "telefono");
                    config.Email = GetPropString(item, "email");
                    config.Direccion = GetPropString(item, "direccion");
                    config.Ciudad = GetPropString(item, "ciudad");
                    config.Provincia = GetPropString(item, "provincia");
                    config.LogoUrl = GetPropString(item, "logo_url");
                    config.BannerUrl = GetPropString(item, "banner_url");
                    config.Horarios = GetPropString(item, "horarios");
                    config.AliasMercadoPago = GetPropString(item, "alias_mercadopago");
                    config.Cbu = GetPropString(item, "cbu");
                    config.InstruccionesPago = GetPropString(item, "instrucciones_pago");
                    
                    return config;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[CONFIG] ERROR: {ex.Message}");
                return new ConfiguracionCarniceria();
            }
        }
        
        private string GetPropString(JsonElement item, string name)
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
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        System.Console.WriteLine($"[CONFIG] ERROR update: {error}");
                        return config;
                    }
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        System.Console.WriteLine($"[CONFIG] UPDATE exitoso (204), haciendo GET...");
                        return await ObtenerConfiguracionAsync();
                    }
                    
                    var responseContent = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "[]")
                        return await ObtenerConfiguracionAsync();
                    
                    try
                    {
                        var configs = await response.Content.ReadFromJsonAsync<List<ConfiguracionCarniceria>>();
                        return configs?.FirstOrDefault() ?? await ObtenerConfiguracionAsync();
                    }
                    catch
                    {
                        return await ObtenerConfiguracionAsync();
                    }
                }
                else
                {
                    var url = "/rest/v1/configuracion_carniceria?select=*";
                    var response = await _httpClient.PostAsync(url, content);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        System.Console.WriteLine($"[CONFIG] ERROR insert: {error}");
                        return config;
                    }
                    
                    var responseContent = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(responseContent) || responseContent.Trim() == "[]")
                        return config;
                    
                    try
                    {
                        var configs = await response.Content.ReadFromJsonAsync<List<ConfiguracionCarniceria>>();
                        return configs?.FirstOrDefault() ?? config;
                    }
                    catch { return config; }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[CONFIG] ERROR: {ex.Message}");
                return new ConfiguracionCarniceria();
            }
        }
    }
}

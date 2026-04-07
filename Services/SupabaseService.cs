using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CarniceriaWhatsApp.Models;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;

namespace CarniceriaWhatsApp.Services
{
    public class SupabaseService : ISupabaseService
    {
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string? _supabaseKey;
        
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
        
        public async Task<Producto?> ObtenerProductoAsync(int id)
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
        
        public async Task<Producto?> ActualizarProductoAsync(int id, Producto producto)
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
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower 
                };
                
                var json = JsonSerializer.Serialize(updateData, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var url = $"/rest/v1/productos_carniceria?id=eq.{id}&select=*";
                var response = await _httpClient.PatchAsync(url, content);
                
                if (response.StatusCode == System.Net.HttpStatusCode.OK || 
                    response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        Console.WriteLine($"[UPDATE] Producto {id} actualizado (204 No Content)");
                        return producto;
                    }
                    
                    try
                    {
                        var result = await response.Content.ReadFromJsonAsync<List<Producto>>();
                        if (result?.Count > 0)
                        {
                            Console.WriteLine($"[UPDATE] Producto {id} actualizado correctamente");
                            return result[0];
                        }
                    }
                    catch (Exception parseEx)
                    {
                        Console.WriteLine($"[UPDATE] Parse error: {parseEx.Message}");
                        return producto;
                    }
                }
                
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[UPDATE ERROR] {response.StatusCode}: {error}");
                return null;
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
                {
                    Console.WriteLine("[STORAGE] Error: No hay API key configurada");
                    return "https://via.placeholder.com/300";
                }
                
                using (var ms = new MemoryStream())
                {
                    await imagenStream.CopyToAsync(ms);
                    var bytes = ms.ToArray();
                    
                    Console.WriteLine($"[STORAGE] Subiendo imagen: {fileName} ({bytes.Length} bytes)");
                    
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
                            {
                                var publicUrl = $"{_supabaseUrl}/storage/v1/object/public/productos-carniceria/{fileName}";
                                Console.WriteLine($"[STORAGE] Imagen subida exitosamente: {publicUrl}");
                                return publicUrl;
                            }
                            else
                            {
                                var errorBody = await response.Content.ReadAsStringAsync();
                                Console.WriteLine($"[STORAGE ERROR] {response.StatusCode}: {errorBody}");
                                return "https://via.placeholder.com/300";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[STORAGE EXCEPTION] {ex.Message}");
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
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.OK || 
                        response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        Console.WriteLine($"[CONFIG] Actualizado correctamente (Status: {response.StatusCode})");
                        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                            return config;
                        
                        var result = await response.Content.ReadFromJsonAsync<List<ConfiguracionCarniceria>>();
                        return result?.Count > 0 ? result[0] : config;
                    }
                    
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[CONFIG ERROR] {response.StatusCode}: {error}");
                    return config;
                }
                else
                {
                    var url = "/rest/v1/configuracion_carniceria?select=*";
                    var response = await _httpClient.PostAsync(url, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<List<ConfiguracionCarniceria>>();
                        return result?.Count > 0 ? result[0] : config;
                    }
                    
                    return config;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CONFIG EXCEPTION] {ex.Message}");
                return new ConfiguracionCarniceria();
            }
        }
        
        // ============================================
        // ✅ MÉTODOS PARA PEDIDOS Y REPORTES
        // ============================================
        
        public async Task<Pedido> CrearPedidoAsync(Pedido pedido)
        {
            try
            {
                Console.WriteLine($"[PEDIDO] Creando pedido para {pedido.NombreCliente} - Total: ${pedido.Total}");
                
                var pedidoData = new
                {
                    nombre_cliente = pedido.NombreCliente,
                    telefono_cliente = pedido.TelefonoCliente,
                    direccion_cliente = pedido.DireccionCliente,
                    notas_cliente = pedido.NotasCliente,
                    total = pedido.Total,
                    estado = pedido.Estado
                };
                
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
                var json = JsonSerializer.Serialize(pedidoData, options);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync("/rest/v1/pedidos?select=*", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[PEDIDO ERROR] {response.StatusCode}: {error}");
                    return pedido;
                }
                
                var result = await response.Content.ReadFromJsonAsync<List<Pedido>>();
                var pedidoCreado = result?.Count > 0 ? result[0] : pedido;
                
                Console.WriteLine($"[PEDIDO] Creado con ID: {pedidoCreado.Id}");
                
                if (pedido.Detalles?.Count > 0 && pedidoCreado.Id != null)
                {
                    foreach (var detalle in pedido.Detalles)
                    {
                        var nombreProducto = string.IsNullOrEmpty(detalle.ProductoNombre) ? "Producto sin nombre" : detalle.ProductoNombre;
                        
                        var detalleData = new
                        {
                            pedido_id = pedidoCreado.Id,
                            producto_id = detalle.ProductoId,
                            producto_nombre = nombreProducto,
                            precio_por_kilo = detalle.PrecioPorKilo,
                            cantidad = detalle.Cantidad,
                            subtotal = detalle.Subtotal
                        };
                        
                        var detalleJson = JsonSerializer.Serialize(detalleData, options);
                        var detalleContent = new StringContent(detalleJson, Encoding.UTF8, "application/json");
                        
                        await _httpClient.PostAsync("/rest/v1/pedido_detalles?select=*", detalleContent);
                    }
                    
                    Console.WriteLine($"[PEDIDO] {pedido.Detalles.Count} detalles agregados");
                }
                
                return pedidoCreado;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PEDIDO EXCEPTION] {ex.Message}");
                return pedido;
            }
        }
        
        public async Task<List<Pedido>> ObtenerPedidosAsync(int dias = 30)
        {
            try
            {
                var fechaDesde = DateTime.Now.AddDays(-dias).ToString("yyyy-MM-dd");
                var response = await _httpClient.GetAsync($"/rest/v1/pedidos?creado_en=gte.{fechaDesde}&order=creado_en.desc");
                
                if (!response.IsSuccessStatusCode) return new List<Pedido>();
                
                return await response.Content.ReadFromJsonAsync<List<Pedido>>() ?? new List<Pedido>();
            }
            catch { return new List<Pedido>(); }
        }
        
        public async Task<List<Pedido>> ObtenerPedidosPorEstadoAsync(string estado)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/rest/v1/pedidos?estado=eq.{estado}&order=creado_en.desc");
                
                if (!response.IsSuccessStatusCode) return new List<Pedido>();
                
                return await response.Content.ReadFromJsonAsync<List<Pedido>>() ?? new List<Pedido>();
            }
            catch { return new List<Pedido>(); }
        }
        
        // ✅ MÉTODO CORREGIDO PARA REPORTES - VENTAS POR DÍA FUNCIONAL
      public async Task<ReporteVentas> ObtenerReporteVentasAsync(int dias = 30)
{
    try
    {
        Console.WriteLine($"[REPORTE] ========== INICIO ==========");
        Console.WriteLine($"[REPORTE] Generando reporte de {dias} días");
        
        var reporte = new ReporteVentas();
        var fechaDesde = DateTime.Now.AddDays(-dias).ToString("yyyy-MM-dd");
        
        // 1. Obtener pedidos
        Console.WriteLine($"[REPORTE] 1️⃣ Consultando pedidos desde {fechaDesde}...");
        var pedidosResponse = await _httpClient.GetAsync($"/rest/v1/pedidos?select=id,total,estado,created_at&created_at=gte.{fechaDesde}&order=created_at.desc");
        
        if (!pedidosResponse.IsSuccessStatusCode) 
        {
            var error = await pedidosResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"[REPORTE ERROR pedidos] {pedidosResponse.StatusCode}: {error}");
            return reporte;
        }
        
        var pedidos = await pedidosResponse.Content.ReadFromJsonAsync<List<Pedido>>() ?? new List<Pedido>();
        
        Console.WriteLine($"[REPORTE] ✅ Pedidos encontrados: {pedidos.Count}");
        foreach(var p in pedidos.Take(5))
        {
            Console.WriteLine($"  📦 Pedido #{p.Id}: ${p.Total} - Fecha: {p.CreadoEn?.ToString("yyyy-MM-dd") ?? "SIN FECHA"}");
        }
        
        reporte.TotalPedidos = pedidos.Count;
        reporte.TotalVentas = pedidos.Sum(p => p.Total);
        reporte.TicketPromedio = pedidos.Count > 0 ? reporte.TotalVentas / pedidos.Count : 0;
        
        // 2. Ventas por día
        Console.WriteLine($"[REPORTE] 2️⃣ Calculando ventas por día...");
        var ventasPorDia = pedidos
            .Where(p => p.CreadoEn.HasValue)
            .GroupBy(p => p.CreadoEn.Value.Date.ToString("yyyy-MM-dd"))
            .Select(g => new VentaPorDia
            {
                Fecha = g.Key,
                Total = g.Sum(p => p.Total),
                Pedidos = g.Count()
            })
            .OrderByDescending(v => v.Fecha)
            .ToList();
        
        Console.WriteLine($"[REPORTE] ✅ Ventas por día: {ventasPorDia.Count} días con ventas");
        reporte.VentasPorDia = ventasPorDia;
        
        // 3. Productos más vendidos - DEBUG EXTENSIVO
        Console.WriteLine($"[REPORTE] 3️⃣ Obteniendo productos más vendidos...");
        
        if (pedidos.Count > 0)
        {
            var validPedidoIds = pedidos
                .Where(p => p.Id != null && p.Id > 0)
                .Select(p => p.Id!.Value)
                .ToList();
            
            Console.WriteLine($"[REPORTE] IDs de pedidos válidos: {validPedidoIds.Count}");
            Console.WriteLine($"[REPORTE] IDs: {string.Join(",", validPedidoIds)}");
            
            if (validPedidoIds.Count > 0)
            {
                var pedidoIdsString = string.Join(",", validPedidoIds);
                
                // ✅ CONSULTA EXPLÍCITA CON TODOS LOS CAMPOS
                var detallesUrl = $"/rest/v1/pedido_detalles?select=id,pedido_id,producto_id,producto_nombre,precio_por_kilo,cantidad,subtotal&pedido_id=in.({pedidoIdsString})";
                Console.WriteLine($"[REPORTE] URL detalles: {detallesUrl}");
                
                var detallesResponse = await _httpClient.GetAsync(detallesUrl);
                
                if (!detallesResponse.IsSuccessStatusCode)
                {
                    var error = await detallesResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"[REPORTE ERROR detalles] {detallesResponse.StatusCode}: {error}");
                }
                else
                {
                    var detalles = await detallesResponse.Content.ReadFromJsonAsync<List<PedidoDetalle>>() ?? new List<PedidoDetalle>();
                    
                    Console.WriteLine($"[REPORTE] ✅ Detalles encontrados: {detalles.Count}");
                    
                    // LOG CADA DETALLE
                    foreach(var d in detalles.Take(10))
                    {
                        Console.WriteLine($"  🥩 Detalle: ID={d.Id}, Pedido={d.PedidoId}, Producto='{d.ProductoNombre}', Cant={d.Cantidad}, Subtotal=${d.Subtotal}");
                    }
                    
                    // FILTRAR Y AGRUPAR
                    var detallesConNombre = detalles
                        .Where(d => !string.IsNullOrEmpty(d.ProductoNombre))
                        .ToList();
                    
                    Console.WriteLine($"[REPORTE] Detalles con nombre no vacío: {detallesConNombre.Count}");
                    
                    var detallesValidos = detallesConNombre
                        .Where(d => d.ProductoNombre != "Producto sin nombre")
                        .ToList();
                    
                    Console.WriteLine($"[REPORTE] Detalles válidos (no 'Producto sin nombre'): {detallesValidos.Count}");
                    
                    var productosMasVendidos = detallesValidos
                        .GroupBy(d => d.ProductoNombre)
                        .Select(g => new ProductoMasVendido
                        {
                            Nombre = g.Key,
                            CantidadVendida = g.Sum(d => d.Cantidad),
                            TotalVendido = g.Sum(d => d.Subtotal),
                            VecesVendido = g.Count()
                        })
                        .OrderByDescending(p => p.TotalVendido)
                        .Take(10)
                        .ToList();
                    
                    Console.WriteLine($"[REPORTE] ✅ Productos únicos agrupados: {productosMasVendidos.Count}");
                    
                    foreach(var prod in productosMasVendidos)
                    {
                        Console.WriteLine($"  🏆 {prod.Nombre}: {prod.CantidadVendida}kg, ${prod.TotalVendido} ({prod.VecesVendido} veces)");
                    }
                    
                    reporte.ProductosMasVendidos = productosMasVendidos;
                }
            }
            else
            {
                Console.WriteLine("[REPORTE] ⚠️ No hay IDs de pedidos válidos");
            }
        }
        else
        {
            Console.WriteLine("[REPORTE] ⚠️ No hay pedidos para procesar");
        }
        
        // 4. Ventas por estado
        var ventasPorEstado = pedidos
            .GroupBy(p => p.Estado ?? "desconocido")
            .Select(g => new VentaPorEstado
            {
                Estado = g.Key,
                Cantidad = g.Count(),
                Total = g.Sum(p => p.Total)
            })
            .ToList();
        
        reporte.VentasPorEstado = ventasPorEstado;
        
        Console.WriteLine($"[REPORTE] ========== FIN ==========");
        Console.WriteLine($"[REPORTE] Total: ${reporte.TotalVentas} | Pedidos: {reporte.TotalPedidos} | Productos: {reporte.ProductosMasVendidos.Count}");
        
        return reporte;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[REPORTE EXCEPTION] {ex.Message}");
        Console.WriteLine($"[REPORTE STACK] {ex.StackTrace}");
        return new ReporteVentas();
    }
}
        
        public async Task<bool> ActualizarEstadoPedidoAsync(long pedidoId, string estado)
        {
            try
            {
                var updateData = new { estado = estado };
                var json = JsonSerializer.Serialize(updateData, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PatchAsync($"/rest/v1/pedidos?id=eq.{pedidoId}", content);
                
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}

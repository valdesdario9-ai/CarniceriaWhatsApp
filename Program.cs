using System;
using CarniceriaWhatsApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ Services
builder.Services.AddRazorPages();
builder.Services.AddHttpClient<ISupabaseService, SupabaseService>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ✅ Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.MapRazorPages();

// ✅ CRÍTICO PARA RENDER: Escuchar en el puerto que Render asigna
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
System.Console.WriteLine($"[RENDER] Binding to port: {port}");
app.Urls.Add($"http://0.0.0.0:{port}");

app.Run();

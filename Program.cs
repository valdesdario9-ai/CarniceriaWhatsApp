using CarniceriaWhatsApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar servicios
builder.Services.AddHttpClient<ISupabaseService, SupabaseService>();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configurar pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();

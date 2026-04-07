using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using CarniceriaWhatsApp.Models;
using CarniceriaWhatsApp.Services;

namespace CarniceriaWhatsApp.Pages.Admin
{
    public class ReportesModel : PageModel
    {
        private readonly ISupabaseService _supabase;
        
        public ReportesModel(ISupabaseService supabase)
        {
            _supabase = supabase;
        }
        
        public ReporteVentas? Reporte { get; set; }
        public int Dias { get; set; } = 30;
        public string? Mensaje { get; set; }
        public bool EsError { get; set; }
        
        public async Task<IActionResult> OnGetAsync(int dias = 30)
        {
            // Validar parámetro
            Dias = dias > 0 ? dias : 30;
            
            try 
            {
                Reporte = await _supabase.ObtenerReporteVentasAsync(Dias);
                
                if (Reporte == null)
                {
                    Reporte = new ReporteVentas();
                    Mensaje = "⚠️ No se pudieron cargar los datos del reporte";
                    EsError = true;
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"[REPORTES ERROR] {ex.Message}");
                Reporte = new ReporteVentas();
                Mensaje = "❌ Error al cargar reportes: " + ex.Message;
                EsError = true;
            }
            
            return Page();
        }
    }
}

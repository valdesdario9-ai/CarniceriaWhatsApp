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
        
        public async Task<IActionResult> OnGetAsync(int dias = 30)
        {
            Dias = dias;
            if (Dias <= 0) Dias = 30;
            
            // Si hay error, retornar reporte vacío en vez de null
            try 
            {
                Reporte = await _supabase.ObtenerReporteVentasAsync(Dias);
            }
            catch 
            {
                Reporte = new ReporteVentas();
            }
            
            return Page();
        }
    }
}

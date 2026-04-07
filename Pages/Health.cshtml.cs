using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarniceriaWhatsApp.Pages
{
    public class HealthModel : PageModel
    {
        public IActionResult OnGet()
        {
            Response.ContentType = "application/json";
            return Content("{\"status\":\"healthy\",\"timestamp\":\"" + System.DateTime.UtcNow + "\"}");
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarniceriaWhatsApp.Pages.Admin
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            // ✅ Solo eliminar sesión de admin, NO la de licencia
            HttpContext.Session.Remove("AdminLogged");
            // NO hacer Session.Clear() para mantener LicenciaActivada_2026-04
            
            // ✅ Redirigir a la tienda
            return Redirect("/");
        }
        
        public IActionResult OnPost() => OnGet();
    }
}

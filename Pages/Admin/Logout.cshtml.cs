using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarniceriaWhatsApp.Pages.Admin
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            // ✅ Eliminar sesión de admin
            HttpContext.Session.Remove("AdminLogged");
            
            // ✅ Eliminar sesión de licencia
            HttpContext.Session.Clear();
            
            // ✅ REDIRIGIR A LA TIENDA (no quedarse en admin)
            return Redirect("/");
        }
        
        public IActionResult OnPost() => OnGet();
    }
}

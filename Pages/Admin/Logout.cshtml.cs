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
            
            // ✅ Redirigir al login
            return RedirectToPage("/Admin/Login");
        }
        
        public IActionResult OnPost()
        {
            return OnGet();
        }
    }
}

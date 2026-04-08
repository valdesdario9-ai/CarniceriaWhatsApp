using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarniceriaWhatsApp.Pages.Admin
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            HttpContext.Session.Remove("AdminLogged");
            return RedirectToPage("/Admin/Login");
        }
        public IActionResult OnPost() => OnGet();
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarniceriaWhatsApp.Pages.Admin
{
    public class LoginModel : PageModel
    {
        public string Message { get; set; } = "";
        
        public IActionResult OnGet()
        {
            return Page();
        }
        
        public IActionResult OnPost(string Username, string Password)
        {
            // ✅ Usuario y contraseña simples (cambiar en producción)
            if (Username == "admin" && Password == "admin123")
            {
                HttpContext.Session.SetString("AdminLogged", "true");
                return RedirectToPage("/Admin/Productos");
            }
            
            Message = "❌ Usuario o contraseña incorrectos";
            return Page();
        }
    }
}

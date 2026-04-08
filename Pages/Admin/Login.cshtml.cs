using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarniceriaWhatsApp.Pages.Admin
{
    public class LoginModel : PageModel
    {
        public string Message { get; set; } = "";
        public bool ShowLicenseReminder { get; set; } = false;
        
        public IActionResult OnGet() => Page();
        
        public IActionResult OnPost(string Username, string Password)
        {
            // ✅ Usuario y contraseña (CAMBIAR en producción)
            if (Username == "admin" && Password == "admin123")
            {
                HttpContext.Session.SetString("AdminLogged", "true");
                
                // ✅ Mostrar recordatorio de licencia después del login
                ShowLicenseReminder = true;
                
                // ✅ Renderizar la misma página con el modal visible
                return Page();
            }
            
            Message = "❌ Usuario o contraseña incorrectos";
            return Page();
        }
    }
}

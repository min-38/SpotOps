using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SpotOps.Features.Auth.Register;

public class ViewModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "회원가입";
    }
}

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SpotOps.Features.Auth.Login;

public class ViewModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "로그인";
    }
}

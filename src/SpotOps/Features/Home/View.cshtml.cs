using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SpotOps.Features.Home;

public class ViewModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "홈";
    }
}

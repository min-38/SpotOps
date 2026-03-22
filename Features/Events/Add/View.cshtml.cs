using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotOps.Models;

namespace SpotOps.Features.Events.Add;

[Authorize(Roles = nameof(UserRole.Organizer))]
public class ViewModel : PageModel
{
    public void OnGet()
    {
        ViewData["Title"] = "공연 등록";
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SpotOps.Features.Events.Detail;

public class ViewModel : PageModel
{
    private readonly EventDetailService _details;

    public ViewModel(EventDetailService details)
    {
        _details = details;
    }

    public EventDetailDto Event { get; private set; } = null!;

    public IActionResult OnGet(Guid id)
    {
        var dto = _details.GetById(id);
        if (dto == null)
            return NotFound();

        Event = dto;
        ViewData["Title"] = dto.Title;
        return Page();
    }
}

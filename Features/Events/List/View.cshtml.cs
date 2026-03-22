using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotOps.Features.Events.ListEvents;

namespace SpotOps.Features.Events.List;

public class ViewModel : PageModel
{
    private readonly ListEventsService _listEvents;

    public ViewModel(ListEventsService listEvents)
    {
        _listEvents = listEvents;
    }

    public IReadOnlyList<EventListRowDto> Events { get; private set; } = [];

    public void OnGet()
    {
        ViewData["Title"] = "공연 목록";
        Events = _listEvents.ListActive();
    }
}

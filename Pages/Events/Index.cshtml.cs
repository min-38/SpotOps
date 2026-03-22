using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotOps.Data;
using SpotOps.Models;

namespace SpotOps.Pages.Events;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public List<Event> Events { get; set; } = [];

    public void OnGet()
    {
        Events = _db.Events
            .Where(e => e.SaleEndAt > DateTime.UtcNow)
            .OrderBy(e => e.EventAt)
            .ToList();
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotOps.Data;
using SpotOps.Models;

namespace SpotOps.Pages.Events;

public class DetailModel : PageModel
{
    private readonly AppDbContext _db;

    public DetailModel(AppDbContext db)
    {
        _db = db;
    }

    public Event Event { get; set; } = null!;

    public IActionResult OnGet(Guid id)
    {
        var ev = _db.Events.FirstOrDefault(e => e.Id == id);

        if (ev == null)
            return NotFound();

        Event = ev;
        return Page();
    }
}
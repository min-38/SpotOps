using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using SpotOps.Data;
using SpotOps.Models;
using System.Security.Claims;

namespace SpotOps.Pages.Organizer;

[Authorize(Roles = "Organizer")]
public class CreateEventModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateEventModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public TicketType TicketType { get; set; }
        public DateTime EventAt { get; set; }
        public DateTime SaleStartAt { get; set; }
        public DateTime SaleEndAt { get; set; }
        public int TotalCapacity { get; set; }
        public decimal Price { get; set; }
        public string VenueName { get; set; } = "";
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var organizer = _db.Organizers.FirstOrDefault(o => o.UserId == userId);

        if (organizer == null)
            return Forbid();

        var ev = new Event
        {
            OrganizerId = organizer.Id,
            Title = Input.Title,
            Description = Input.Description,
            TicketType = Input.TicketType,
            EventAt = Input.EventAt.ToUniversalTime(),
            SaleStartAt = Input.SaleStartAt.ToUniversalTime(),
            SaleEndAt = Input.SaleEndAt.ToUniversalTime(),
            TotalCapacity = Input.TotalCapacity,
            Price = Input.Price,
            VenueName = Input.VenueName
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return RedirectToPage("/Index");
    }
}
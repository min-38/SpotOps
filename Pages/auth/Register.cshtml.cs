using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpotOps.Data;
using SpotOps.Models;

namespace SpotOps.Pages.auth;

public class RegisterModel : PageModel
{
    private readonly AppDbContext _db;

    public RegisterModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Phone { get; set; }
        public UserRole Role { get; set; } = UserRole.Buyer;
        public string? BusinessNumber { get; set; }
        public string? CompanyName { get; set; }
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        // 이메일 중복 체크
        if (_db.Users.Any(u => u.Email == Input.Email))
        {
            ModelState.AddModelError("Input.Email", "이미 사용 중인 이메일이에요.");
            return Page();
        }

        var user = new User
        {
            Email = Input.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Input.Password),
            Name = Input.Name,
            Phone = Input.Phone,
            Role = Input.Role
        };

        _db.Users.Add(user);

        // 주최자면 Organizer도 생성
        if (Input.Role == UserRole.Organizer)
        {
            var organizer = new Organizer
            {
                UserId = user.Id,
                BusinessNumber = Input.BusinessNumber ?? "",
                CompanyName = Input.CompanyName ?? "",
                IsVerified = false
            };
            _db.Organizers.Add(organizer);
        }

        await _db.SaveChangesAsync();

        return RedirectToPage("/auth/Login");
    }
}
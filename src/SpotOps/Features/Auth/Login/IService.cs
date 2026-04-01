using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SpotOps.Data;
using SpotOps.Models;

namespace SpotOps.Features.Auth.Login;

public interface ILoginService
{
    Task<User?> ValidateAsync(string email, string password, CancellationToken ct = default);
    Task<JWTResponse> CreateTokenPairAsync(User user, CancellationToken ct = default);
    Task<(User? User, JWTResponse? Tokens, string? ErrorCode)> RefreshTokenAsync(string rawToken, CancellationToken ct = default);
}

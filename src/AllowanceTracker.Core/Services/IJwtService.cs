using AllowanceTracker.Models;
using System.Security.Claims;

namespace AllowanceTracker.Services;

public interface IJwtService
{
    string GenerateToken(ApplicationUser user);
    bool ValidateToken(string token);
    ClaimsPrincipal? GetPrincipalFromToken(string token);
}

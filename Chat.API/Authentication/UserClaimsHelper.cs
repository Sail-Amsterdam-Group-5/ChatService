using System.Security.Claims;

namespace Chat.API.Authentication;

public static class UserClaimsHelper
{
    public static string GetUserId(this ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User ID not found");
        }
        return userId;
    }

    public static string GetUserRole(this ClaimsPrincipal user)
    {
        var roles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value);

        return roles.FirstOrDefault(r =>
            r == "admin" ||
            r == "team-lead" ||
            r == "volunteer") ?? "volunteer";
    }

    public static bool IsInRole(this ClaimsPrincipal user, string role)
    {
        return user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Any(c => c.Value.Equals(role, StringComparison.OrdinalIgnoreCase));
    }
}
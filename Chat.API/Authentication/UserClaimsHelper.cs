// Chat.API/Authentication/UserClaimsHelper.cs
using System.Security.Claims;

namespace Chat.API.Authentication;

public static class UserClaimsHelper
{
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
        throw new UnauthorizedAccessException("User ID not found in token");

    public static bool GetNotificationPreference(this ClaimsPrincipal user) =>
        bool.TryParse(
            user.FindFirst(JwtClaimConstants.NotificationPreference)?.Value,
            out bool preference) && preference;

    public static string GetUserRole(this ClaimsPrincipal user) =>
        user.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .FirstOrDefault(r =>
                r == "volunteer" ||
                r == "team-lead" ||
                r == "coordinator" ||
                r == "admin") ?? "volunteer";
}
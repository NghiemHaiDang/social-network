using System.Security.Claims;
using BuildingBlocks.Common.Constants;

namespace BuildingBlocks.Common.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst(AppConstants.Claims.UserId)?.Value;
    }

    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value
            ?? principal.FindFirst(AppConstants.Claims.Email)?.Value;
    }

    public static string? GetUserName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Name)?.Value
            ?? principal.FindFirst(AppConstants.Claims.UserName)?.Value;
    }

    public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }

    public static bool IsInRole(this ClaimsPrincipal principal, string role)
    {
        return principal.GetRoles().Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(AppConstants.Roles.Admin);
    }
}

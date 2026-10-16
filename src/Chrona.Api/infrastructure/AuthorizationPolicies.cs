using System.Security.Claims;
using System.Text.Json;

namespace Chrona.Api.Infrastructure;

public static class AuthorizationPolicies
{
    public const string Manager = "Manager";
    public const string Employee = "Employee"; // Employee OU Manager : Manager hérite de tout ce que fait Employee
}

public static class ClaimsPrincipalExtensions
{
    // Keycloak place les rôles du realm dans le claim JSON "realm_access", pas dans un claim "role" plat.
    public static bool HasRealmRole(this ClaimsPrincipal user, string role)
    {
        var claim = user.FindFirst("realm_access");
        if (claim is null) return false;

        try
        {
            using var doc = JsonDocument.Parse(claim.Value);
            return doc.RootElement.TryGetProperty("roles", out var roles)
                   && roles.ValueKind == JsonValueKind.Array
                   && roles.EnumerateArray().Any(r => r.GetString() == role);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
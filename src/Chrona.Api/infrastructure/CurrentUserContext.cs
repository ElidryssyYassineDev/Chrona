using Chrona.Shared.Application;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chrona.Api.Infrastructure;

public class CurrentUserContext : ICurrentUserContext
{
    private sealed class RealmAccess
    {
        [JsonPropertyName("roles")]
        public string[] Roles { get; set; } = [];
    }

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid SubjectId =>
        Guid.Parse(
            _httpContextAccessor
                .HttpContext?
                .User
                .FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User identifier not found"));

    public IReadOnlyCollection<string> Roles => GetRoles();

    private IReadOnlyCollection<string> GetRoles()
    {
        var claim = _httpContextAccessor
            .HttpContext?
            .User
            .FindFirst("realm_access");

        if (claim is null)
        {
            return [];
        }

        var realmAccess = JsonSerializer.Deserialize<RealmAccess>(claim.Value);

        return realmAccess?.Roles ?? [];
    }
}
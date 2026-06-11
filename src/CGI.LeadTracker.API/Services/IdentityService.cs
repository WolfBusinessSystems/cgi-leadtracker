using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CGI.LeadTracker.API.Application.Adapters;

namespace CGI.LeadTracker.API.Services;

public class IdentityService : IIdentityService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdentityService(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public Guid GetUserId()
    {
        var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    public string GetName() =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Name) ?? string.Empty;

    public string GetEmail() =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? string.Empty;

    public string GetRole() =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
}

using Microsoft.AspNetCore.Http;
using SopalTrace.Application.Interfaces;
using System.Security.Claims;

namespace SopalTrace.Infrastructure.Services.Security;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? Matricule => _httpContextAccessor.HttpContext?.User?.FindFirst("matricule")?.Value;

    public string? NomComplet => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;

    public string UserInfo
    {
        get
        {
            var m = Matricule;
            var n = NomComplet;

            if (string.IsNullOrEmpty(m) && string.IsNullOrEmpty(n)) return "SYSTEM";
            if (string.IsNullOrEmpty(m)) return n!;
            if (string.IsNullOrEmpty(n)) return m;

            return $"{m}-{n}";
        }
    }
}

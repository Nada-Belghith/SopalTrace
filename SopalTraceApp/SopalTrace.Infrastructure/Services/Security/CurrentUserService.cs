using SopalTrace.Domain.Exceptions;
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

    public string MatriculeRequis =>
        string.IsNullOrEmpty(Matricule) ? throw new InvalidTokenException() : Matricule;
}

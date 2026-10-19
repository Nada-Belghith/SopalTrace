using System.Security.Claims;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Matricule de l'utilisateur authentifié. L'identité de l'opérateur est toujours lue depuis le jeton :
    /// une valeur envoyée par le client ne doit jamais être utilisée pour tracer un contrôle.
    /// </summary>
    public static string GetMatricule(this ClaimsPrincipal user)
    {
        var matricule = user.FindFirst("matricule")?.Value;
        if (string.IsNullOrWhiteSpace(matricule))
            throw new InvalidTokenException();
        return matricule;
    }
}

/// <summary>Formats acceptés pour les codes reçus dans l'URL (poste, machine...).</summary>
public static class FormatsEntree
{
    public const string Code = @"^[A-Za-z0-9 _.\-/]{1,30}$";
    public const string MessageCodeInvalide = "Code invalide.";
}

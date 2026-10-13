using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services;

internal static class SessionsUtilisateur
{
    /// <summary>
    /// Ferme toutes les sessions du compte : nouveau tampon de sécurité (les jetons d'accès déjà émis sont refusés),
    /// révocation des jetons de rafraîchissement et oubli de l'état mis en cache.
    /// </summary>
    public static async Task FermerAsync(UtilisateursApp user, IUserRepository repository, IUserSessionValidator validator)
    {
        user.SecurityStamp = Guid.NewGuid();
        await repository.UpdateUserAsync(user);
        await repository.RevokeAllTokensForUserAsync(user.Id);
        validator.Invalidate(user.Id);
    }
}

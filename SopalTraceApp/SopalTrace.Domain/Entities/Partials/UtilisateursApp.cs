using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Domain.Entities;

// Complément du code généré (Database First) : ce fichier n'est pas écrasé par le scaffold.
public partial class UtilisateursApp
{
    /// <summary>Statut par défaut identique au DEFAULT SQL : une inscription est d'abord une demande.</summary>
    public UtilisateursApp()
    {
        AccountStatus = AccountStates.PendingApproval;
    }

    /// <summary>
    /// Seul point de changement du statut : la transition doit figurer dans <see cref="AccountStates"/>.
    /// Les conditions de la base (rôle obligatoire, mot de passe d'un compte actif) sont vérifiées ici
    /// pour donner un message clair plutôt qu'une erreur SQL.
    /// </summary>
    public void PasserA(string nouveauStatut)
    {
        if (!AccountStates.PeutPasser(AccountStatus, nouveauStatut))
            throw new RegleMetierException(
                $"Action impossible : le compte {Matricule} est « {AccountStates.Libelle(AccountStatus)} » et ne peut pas passer à « {AccountStates.Libelle(nouveauStatut)} ».");

        if (AccountStates.RoleObligatoire(nouveauStatut) && string.IsNullOrEmpty(RoleApp))
            throw new RegleMetierException($"Un rôle doit être attribué au compte {Matricule} avant de lui donner accès.");

        if (nouveauStatut == AccountStates.Active && string.IsNullOrEmpty(MotDePasseHash))
            throw new RegleMetierException($"Le compte {Matricule} ne peut être actif qu'une fois son mot de passe défini.");

        AccountStatus = nouveauStatut;
    }
}

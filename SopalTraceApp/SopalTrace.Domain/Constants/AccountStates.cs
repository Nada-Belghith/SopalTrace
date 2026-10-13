using System;
using System.Collections.Generic;
using System.Linq;

namespace SopalTrace.Domain.Constants;

/// <summary>
/// Cycle de vie d'un compte utilisateur (colonne UtilisateursApp.AccountStatus).
/// <code>
///   (inscription) ─► PENDING_APPROVAL ─┬─► INVITED ──► ACTIVE ◄─┐
///   (rôle pré-attribué) ───────────────┘    │  ▲          │      │
///                     │                     ▼  │          ▼      │
///                     └─► REJECTED ─► INVITED  └──── DISABLED ───┘
/// </code>
/// Seules les transitions listées ici sont possibles ; toute autre demande est refusée.
/// </summary>
public static class AccountStates
{
    /// <summary>Demande d'accès déposée par l'employé, en attente de la décision d'un administrateur.</summary>
    public const string PendingApproval = "PENDING_APPROVAL";
    /// <summary>Accès accordé : lien d'activation envoyé, mot de passe pas encore défini.</summary>
    public const string Invited = "INVITED";
    public const string Active = "ACTIVE";
    /// <summary>Demande refusée par un administrateur (motif obligatoire).</summary>
    public const string Rejected = "REJECTED";
    /// <summary>Compte suspendu par un administrateur.</summary>
    public const string Disabled = "DISABLED";

    public static readonly string[] All = { PendingApproval, Invited, Active, Rejected, Disabled };

    private static readonly Dictionary<string, string[]> Transitions = new()
    {
        [PendingApproval] = new[] { Invited, Rejected },
        // Un refus peut être reconsidéré : l'administrateur accorde alors l'accès.
        [Rejected] = new[] { Invited },
        // INVITED → INVITED : nouveau lien d'activation (lien expiré ou perdu).
        [Invited] = new[] { Invited, Active, Disabled },
        [Active] = new[] { Disabled },
        // Réactivation : ACTIVE si le mot de passe est déjà défini, sinon nouvelle invitation.
        [Disabled] = new[] { Active, Invited }
    };

    public static bool PeutPasser(string depuis, string vers)
        => Transitions.TryGetValue(depuis, out var cibles) && cibles.Contains(vers);

    /// <summary>Statuts depuis lesquels on peut rejoindre <paramref name="vers"/>.</summary>
    public static IEnumerable<string> Origines(string vers)
        => Transitions.Where(t => t.Value.Contains(vers)).Select(t => t.Key);

    /// <summary>Seul un compte ACTIVE peut ouvrir une session.</summary>
    public static bool PeutSeConnecter(string statut) => statut == Active;

    /// <summary>Le rôle n'est obligatoire qu'une fois l'accès accordé.</summary>
    public static bool RoleObligatoire(string statut) => statut is not (PendingApproval or Rejected);

    public static string Libelle(string statut) => statut switch
    {
        PendingApproval => "demande en attente",
        Invited => "invitation envoyée",
        Active => "actif",
        Rejected => "demande refusée",
        Disabled => "désactivé",
        _ => throw new ArgumentOutOfRangeException(nameof(statut), statut, "Statut de compte inconnu.")
    };
}

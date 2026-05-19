using System;

namespace SopalTrace.Domain.Interfaces;

/// <summary>
/// Interface contrat pour les entités de plan qualité (génériques et spécifiques).
/// Définit les propriétés communes à tous les plans.
/// </summary>
public interface IPlanEntete
{
    /// <summary>
    /// Identifiant unique du plan.
    /// </summary>
    Guid Id { get; set; }

    /// <summary>
    /// Statut du plan (ex: "ACTIF", "BROUILLON", "ARCHIVE").
    /// </summary>
    string Statut { get; set; }

    /// <summary>
    /// Numéro de version du plan.
    /// </summary>
    int Version { get; set; }


    /// <summary>
    /// Utilisateur ayant créé le plan.
    /// </summary>
    string CreePar { get; set; }

    /// <summary>
    /// Date et heure de création.
    /// </summary>
    DateTime CreeLe { get; set; }
}

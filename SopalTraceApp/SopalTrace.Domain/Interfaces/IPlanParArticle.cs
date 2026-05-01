using System;

namespace SopalTrace.Domain.Interfaces;

/// <summary>
/// Interface contrat pour les plans instanciés pour un article Sage spécifique.
/// Représente les plans par article (spécifiques) avec suivi du brouillon et sauvegarde automatique.
/// </summary>
public interface IPlanParArticle : IPlanEntete
{
    /// <summary>
    /// Code article Sage auquel ce plan est associé.
    /// </summary>
    string CodeArticleSage { get; set; }

    /// <summary>
    /// Date et heure de la dernière sauvegarde automatique du plan.
    /// </summary>
}

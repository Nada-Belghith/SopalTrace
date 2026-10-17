using System;
using System.Collections.Generic;
using System.Linq;
using SopalTrace.Domain.Constants;

namespace SopalTrace.Application.Rapports;

/// <summary>Affectation d'une version de modèle (historique ReportTemplateAssignment), dans l'ordre de création de la base.</summary>
public sealed record AffectationModele(Guid VersionId, string CodeModele, int NumeroVersion, string Nature, Guid? FormulaireId, long Ordre);

/// <summary>
/// Ligne de formulaire (Ref_Formulaire, code + version) : empreinte de ses colonnes propres
/// (ColonneJsonMapper.SignatureColonnes : mêmes colonnes, même empreinte), identifiant commun aux lignes du formulaire
/// (GroupeId : ses versions et ses changements de code) et ordre de création dans la base (FEMANU.1 avant DEMANU.1).
/// </summary>
public sealed record VersionFormulaire(Guid Id, string CodeReference, int Version, string Structure, Guid GroupeId, long Ordre);

/// <summary>Pourquoi ce modèle met en forme le document.</summary>
public enum RegleModele
{
    /// <summary>Modèle de la version du formulaire du document, en vigueur à sa création.</summary>
    EnVigueurALaCreation,
    /// <summary>Modèle rattaché à la version du formulaire du document après la création du document.</summary>
    ConcuPourLaVersionDuFormulaire,
    /// <summary>Modèle de la version précédente du formulaire (mêmes colonnes), en vigueur à la création du document.</summary>
    HeriteDeLaVersionPrecedente,
    /// <summary>Aucun modèle n'existait à la création du document : premier modèle rattaché à ce formulaire (mêmes colonnes).</summary>
    PremierModele
}

/// <summary>Pourquoi un document n'a pas de modèle (son PDF est refusé).</summary>
public enum ModeleManquant
{
    /// <summary>La version du formulaire a changé de colonnes : un nouveau modèle doit lui être rattaché.</summary>
    StructureModifiee,
    /// <summary>Aucun modèle n'est rattaché à ce formulaire.</summary>
    AucunModele,
    /// <summary>Le document n'a pas de formulaire, et son type n'a pas de modèle par défaut.</summary>
    SansFormulaire
}

public sealed record ModeleResolu(AffectationModele Affectation, RegleModele Regle);

/// <summary>Modèle d'un document, ou la raison de son absence.</summary>
public sealed record ResolutionModele(ModeleResolu? Modele, ModeleManquant? Manque)
{
    public static ResolutionModele Trouve(ModeleResolu modele) => new(modele, null);
    public static ResolutionModele Absent(ModeleManquant manque) => new(null, manque);
}

/// <summary>
/// Modèle qui met en forme un document, déduit de son ordre de création et de l'historique des rattachements : un
/// document garde toujours le modèle en vigueur à sa création. L'ordre est celui de la séquence de la base (aucune
/// comparaison d'horloges). Chaque version de formulaire a son modèle ; les versions consécutives d'un formulaire qui ont
/// les mêmes colonnes forment un groupe qui partage ses modèles ; une version dont les colonnes changent commence un
/// nouveau groupe et n'hérite de rien. Règles, dans l'ordre :
/// <list type="number">
/// <item>dernier modèle rattaché à la version du formulaire du document avant sa création ;</item>
/// <item>premier modèle conçu pour cette version (structure), même rattaché après : la structure du document a pu être
/// créée avant son modèle (colonne ajoutée, puis modèle envoyé) ;</item>
/// <item>dernier modèle rattaché, avant la création du document, à une version précédente du groupe ;</item>
/// <item>premier modèle rattaché à une version du groupe (document créé avant tout modèle) ;</item>
/// <item>sinon aucun modèle : la version a changé de colonnes, ou rien n'est rattaché au formulaire.</item>
/// </list>
/// Un document sans formulaire (registre) prend le modèle par défaut en vigueur à sa création, sinon le premier.
/// </summary>
public static class ResolutionModeleRapport
{
    /// <summary>Ordre d'un document qui serait créé maintenant : « modèle actuel ».</summary>
    public const long Maintenant = long.MaxValue;

    /// <param name="ordreDocument">Ordre de création du document (<see cref="Maintenant"/> pour le modèle actuel).</param>
    /// <param name="formulaire">Version de formulaire du document.</param>
    /// <param name="versionsDuCode">Toutes les versions du formulaire du document (même identifiant de groupe).</param>
    /// <param name="affectations">Rattachements des modèles qui mettent en forme ce type de document.</param>
    public static ResolutionModele PourFormulaire(long ordreDocument, VersionFormulaire formulaire,
        IReadOnlyCollection<VersionFormulaire> versionsDuCode, IReadOnlyCollection<AffectationModele> affectations)
    {
        var groupe = GroupeMemesColonnes(formulaire, versionsDuCode);
        var duFormulaire = affectations.Where(a => a.FormulaireId == formulaire.Id).ToList();
        var precedentes = groupe.Where(v => v.Ordre < formulaire.Ordre).Select(v => v.Id).ToHashSet();
        var duGroupe = groupe.Select(v => v.Id).ToHashSet();

        var resolu = DernierAvant(duFormulaire, ordreDocument, RegleModele.EnVigueurALaCreation)
                     ?? Premier(duFormulaire.Where(a => a.Nature == TypesAffectationModele.StructureFormulaire), RegleModele.ConcuPourLaVersionDuFormulaire)
                     ?? DernierAvant(affectations.Where(a => a.FormulaireId is { } id && precedentes.Contains(id)), ordreDocument, RegleModele.HeriteDeLaVersionPrecedente)
                     ?? Premier(affectations.Where(a => a.FormulaireId is { } id && duGroupe.Contains(id)), RegleModele.PremierModele);
        if (resolu != null) return ResolutionModele.Trouve(resolu);

        var precedente = versionsDuCode
            .Where(v => SontMemeFormulaire(v, formulaire) && v.Ordre < formulaire.Ordre)
            .OrderByDescending(v => v.Ordre)
            .FirstOrDefault();
        return ResolutionModele.Absent(precedente != null && precedente.Structure != formulaire.Structure
            ? ModeleManquant.StructureModifiee
            : ModeleManquant.AucunModele);
    }

    /// <param name="ordreDocument">Ordre de création du document (<see cref="Maintenant"/> pour le modèle actuel).</param>
    /// <param name="parDefaut">Affectations par défaut des modèles de ce type de document (sans formulaire).</param>
    public static ResolutionModele SansFormulaire(long ordreDocument, IReadOnlyCollection<AffectationModele> parDefaut)
    {
        var defauts = parDefaut.Where(a => a.Nature == TypesAffectationModele.ParDefaut).ToList();
        var resolu = DernierAvant(defauts, ordreDocument, RegleModele.EnVigueurALaCreation) ?? Premier(defauts, RegleModele.PremierModele);
        return resolu != null ? ResolutionModele.Trouve(resolu) : ResolutionModele.Absent(ModeleManquant.SansFormulaire);
    }

    /// <summary>Lignes consécutives du formulaire qui ont les mêmes colonnes que lui (lui compris), dans leur ordre de création.</summary>
    public static List<VersionFormulaire> GroupeMemesColonnes(VersionFormulaire formulaire, IReadOnlyCollection<VersionFormulaire> versionsDuCode)
    {
        var versions = versionsDuCode.Where(v => SontMemeFormulaire(v, formulaire) && v.Id != formulaire.Id)
            .Append(formulaire)
            .OrderBy(v => v.Ordre)
            .ToList();
        var position = versions.IndexOf(formulaire);
        var debut = position;
        while (debut > 0 && versions[debut - 1].Structure == formulaire.Structure) debut--;
        var fin = position;
        while (fin < versions.Count - 1 && versions[fin + 1].Structure == formulaire.Structure) fin++;
        return versions.GetRange(debut, fin - debut + 1);
    }

    private static bool SontMemeFormulaire(VersionFormulaire a, VersionFormulaire b) => a.GroupeId == b.GroupeId;

    private static ModeleResolu? DernierAvant(IEnumerable<AffectationModele> affectations, long ordre, RegleModele regle) =>
        affectations.Where(a => a.Ordre < ordre).OrderByDescending(a => a.Ordre).Select(a => new ModeleResolu(a, regle)).FirstOrDefault();

    private static ModeleResolu? Premier(IEnumerable<AffectationModele> affectations, RegleModele regle) =>
        affectations.OrderBy(a => a.Ordre).Select(a => new ModeleResolu(a, regle)).FirstOrDefault();
}

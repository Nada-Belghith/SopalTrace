using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using SopalTrace.Application.Interfaces.Rapports;

namespace SopalTrace.Application.Rapports.Execution;

/// <summary>
/// Contrat d'un type de document d'exécution (Exec_ControleDocumentStatut : fiche d'échantillonnage, contrôle au
/// poste…) : les valeurs sont les réponses des opérateurs, lues par les mêmes services que les écrans.
/// </summary>
public interface IDefinitionRapportExecution : IContratRapport
{
    /// <summary>Type d'exécution (TypesDocumentExecution).</summary>
    string TypeExecution { get; }

    /// <summary>Données du document d'exécution, ou null s'il n'est pas (ou plus) lisible.</summary>
    Task<DonneesExecution?> ChargerAsync(Guid execControleDocumentStatutId);

    /// <summary>
    /// Document qualité (TypesDocument) dont cette fiche reçoit les réponses : sa fiche vierge se consulte depuis le
    /// document. Null : pas de fiche vierge (registre).
    /// </summary>
    string? TypeDocumentApplique => null;

    /// <summary>Ligne de formulaire qui désigne le modèle de la fiche vierge du document (celle d'une fiche créée maintenant).</summary>
    Task<Guid?> FormulaireFicheViergeAsync(Guid documentEnteteId) => Task.FromResult<Guid?>(null);

    /// <summary>Fiche vierge : les données du document (en-tête, lignes, sections), les réponses des opérateurs vides.</summary>
    Task<DonneesExecution?> ChargerViergeAsync(Guid documentEnteteId, FormulaireDuDocument? formulaire) =>
        Task.FromResult<DonneesExecution?>(null);
}

/// <summary>Données à mettre en forme, nom du fichier (sans extension) et variante du modèle à utiliser (ex. code machine).</summary>
public sealed record DonneesExecution(DonneesRapport Donnees, string NomFichier);

/// <summary>Informations communes d'un document d'exécution : OF, article, poste, formulaire du document appliqué.</summary>
public sealed record StatutExecutionRapport(
    Guid Id,
    Guid ExecControleOfId,
    string TypeDocument,
    string? PosteCode,
    string? DesignationPoste,
    string? MachineCode,
    string? Equipe,
    DateTime? DateExecution,
    DateTime? DateTermine,
    bool EstTermine,
    string? MatriculeOperateur,
    string NumeroOf,
    string? CodeArticle,
    string? DesignationArticle,
    string? FamilleCode,
    string? DesignationFamille,
    string? TypeRobinetCode,
    decimal QuantiteLancee,
    DateTime? DateDebutOf,
    string? CodeFormulaire,
    int? VersionFormulaire,
    string? NomDocument,
    Guid? SessionId = null);

/// <summary>Section d'un plan à sections (plan d'assemblage) et ses caractéristiques.</summary>
public sealed record SectionPlanRapport(Guid Id, int Ordre, string Libelle, string? TypeSection, IReadOnlyList<LignePlanRapport> Lignes);

public sealed record LignePlanRapport(Guid Id, int Ordre, string Caracteristique);

/// <summary>Réponse saisie sur une ligne de plan (contrôle de lot : première / dernière pièce, réglage, 100 %).</summary>
public sealed record ReponseLigneRapport(Guid LigneId, string Contexte, string? NumeroReglage, string ResultatType, string? DetailsNc,
    string? ActionsCorrection, string MatriculeOperateur, DateTime DateSaisie);

/// <summary>Contrôle de production planifié (occurrence) répondu sur le poste.</summary>
public sealed record OccurrenceRapport(string TrancheHoraire, string? Resultat, DateTime? HeureReponse);

/// <summary>Ligne active (non annulée) du registre de traçabilité d'un poste : un changement de lots.</summary>
public sealed record EntreeRegistreRapport(int Version, DateTime DateHeure, string? Corps, string? Volant, string? Equipe,
    IReadOnlyList<ComposantRegistreRapport> Composants);

public sealed record ComposantRegistreRapport(string? CodeArticle, string Designation, string? Lot);

/// <summary>Étiquette de composant scannée par le magasin pour l'OF.</summary>
public sealed record EtiquetteRapport(string CodeArticle, string? Designation, string Lot, decimal? Quantite, string? NumeroRapport, DateTime? DateScan);

/// <summary>Bilan d'une tranche horaire de l'équipe (non-conformité, actions, approbation).</summary>
public sealed record TrancheRapport(string TrancheHoraire, string? ResultatFinal, string? DetailsNc, string? ActionsCorrection, string? MatriculeApprobateur);

/// <summary>Plan de vérification machine appliqué : familles de corps (colonnes) et points (risques ou conformités) avec leurs échéances.</summary>
/// <summary>Plan de vérification d'une machine ; TablesEtalonSeparees : les contrôles d'étanchéité forment un tableau à part.</summary>
public sealed record PlanVerifMachineRapport(string MachineCode, string? LibelleMachine, string? ModeleVerification, bool TablesEtalonSeparees,
    IReadOnlyList<FamilleVerifMachine> Familles, IReadOnlyList<PointVerifMachine> Points);

public sealed record FamilleVerifMachine(Guid Id, int Ordre, string Code, string Designation);

public sealed record PointVerifMachine(Guid Id, int Ordre, string? TypeLigne, string Libelle, string? Methode, IReadOnlyList<EcheanceVerifMachine> Echeances);

public sealed record EcheanceVerifMachine(Guid Id, int Ordre, string Periodicite, int OrdrePeriodicite, string? MoyenDetection,
    IReadOnlyList<PieceVerifMachine> Pieces);

/// <summary>Pièce de référence d'une échéance : par famille de corps (FamilleId) ou sans famille (fuite étalon…).</summary>
public sealed record PieceVerifMachine(string? Role, Guid? FamilleId, string? Code);

/// <summary>Paramètres ISO 2859-1 d'un plan d'échantillonnage (sans la taille du lot, propre à chaque OF).</summary>
public sealed record ParametresEchantillonnageRapport(string NiveauControle, string TypePlan, string ModeControle, decimal? Nqa,
    int CritereAcceptation, int CritereRejet);

/// <summary>Dernière réponse saisie pour une échéance.</summary>
public sealed record ReponseVerifMachine(Guid EcheanceId, DateTime DateExecution, string MatriculeOperateur, decimal? PressionEntree,
    decimal? FuiteAffichee, bool? Conforme, string? Observation);

/// <summary>Lecture des informations communes des documents d'exécution (sans écriture).</summary>
public interface IDonneesExecutionRepository
{
    Task<StatutExecutionRapport?> GetStatutAsync(Guid execControleDocumentStatutId);
    /// <summary>
    /// En-tête d'une fiche vierge du document qualité : poste, machine, famille et nom du document ; ni OF ni article
    /// ni équipe (remplis par la production) ; code et version du formulaire de la fiche.
    /// </summary>
    Task<StatutExecutionRapport?> GetStatutViergeAsync(Guid documentEnteteId, string typeExecution, FormulaireDuDocument? formulaire);
    /// <summary>Ligne de formulaire du document qualité.</summary>
    Task<Guid?> GetFormulaireDocumentAsync(Guid documentEnteteId);
    /// <summary>Formulaire de résultats d'assemblage d'un plan d'assemblage (le plus précis pour sa famille et son poste).</summary>
    Task<Guid?> GetFormulaireResultatsPlanAsync(Guid documentEnteteId);
    /// <summary>Paramètres du plan d'échantillonnage (document qualité PLAN_ECHANTILLONNAGE).</summary>
    Task<ParametresEchantillonnageRapport?> GetParametresEchantillonnageAsync(Guid documentEnteteId);
    /// <summary>Structure du plan de vérification machine appliqué (document qualité VERIF_MACHINE).</summary>
    Task<PlanVerifMachineRapport?> GetPlanVerifMachineAsync(Guid documentEnteteId);
    /// <summary>Dernière réponse de chaque échéance d'une vérification machine.</summary>
    Task<List<ReponseVerifMachine>> GetReponsesVerifMachineAsync(Guid execControleDocumentStatutId);
    /// <summary>Document qualité appliqué par le document d'exécution (DocId).</summary>
    Task<Guid?> GetDocumentAppliqueAsync(Guid execControleDocumentStatutId);
    /// <summary>Sections et caractéristiques d'un plan à sections.</summary>
    Task<List<SectionPlanRapport>> GetSectionsPlanAsync(Guid documentEnteteId);
    /// <summary>Réponses saisies sur les lignes du plan pour ce document d'exécution.</summary>
    Task<List<ReponseLigneRapport>> GetReponsesLignesAsync(Guid execControleDocumentStatutId);
    /// <summary>Contrôles de production répondus de l'équipe (session) sur le poste.</summary>
    Task<List<OccurrenceRapport>> GetOccurrencesAsync(Guid execControleOfId, string posteCode, Guid? sessionId);
    /// <summary>Bilans des tranches horaires de l'équipe (session) sur l'OF.</summary>
    Task<List<TrancheRapport>> GetTranchesAsync(Guid execControleOfId, Guid? sessionId);
    /// <summary>
    /// Document de registre qui porte le PDF du registre d'un poste : le premier (les suivants, confirmations des autres
    /// équipes, désignent le même registre).
    /// </summary>
    Task<Guid?> GetPremierStatutRegistreAsync(Guid execControleOfId, string posteCode);
    /// <summary>Lignes actives du registre de traçabilité du poste, dans l'ordre chronologique.</summary>
    Task<List<EntreeRegistreRapport>> GetRegistreAsync(Guid execControleOfId, string posteCode);
    /// <summary>Étiquettes des composants scannées par le magasin pour l'OF.</summary>
    Task<List<EtiquetteRapport>> GetEtiquettesAsync(string numeroOf);
    /// <summary>Nom complet des employés (annuaire), par matricule.</summary>
    Task<Dictionary<string, string>> GetNomsAsync(IEnumerable<string> matricules);
}

/// <summary>Mise en forme commune des valeurs des documents d'exécution.</summary>
internal static class FormatsRapport
{
    public static readonly CultureInfo Francais = CultureInfo.GetCultureInfo("fr-FR");

    public static string Date(DateTime? date) => date?.ToString("dd/MM/yyyy", Francais) ?? "";

    public static string Nombre(int valeur) => valeur.ToString(Francais);

    /// <summary>Tranche « 06-07 » affichée « 6h-7h ».</summary>
    public static string Tranche(string tranche)
    {
        var parties = tranche.Split('-');
        return parties.Length == 2 && int.TryParse(parties[0], out var debut) && int.TryParse(parties[1], out var fin)
            ? $"{debut}h-{fin}h"
            : tranche;
    }

    public static string Croix(bool coche) => coche ? "X" : "";

    /// <summary>Texte sans espaces superflus (vide si absent).</summary>
    public static string Texte(string? valeur) => valeur?.Trim() ?? "";
}

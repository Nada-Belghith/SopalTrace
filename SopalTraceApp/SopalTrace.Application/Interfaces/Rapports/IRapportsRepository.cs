using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Rapports;
using SopalTrace.Application.Rapports;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Rapports;

/// <summary>Version de formulaire, son rôle et ses colonnes propres actives (clé → libellé).</summary>
public sealed record ColonnesFormulaire(Guid Id, string CodeFormulaire, int Version, string Statut, string? Role, IReadOnlyDictionary<string, string> LibellesParCle)
{
    /// <summary>Identifiant commun à toutes les versions du formulaire.</summary>
    public Guid GroupeId { get; init; }
    /// <summary>Champ d'application lisible (« famille RBGFA_SOUPAPE · poste PAS71 »), vide si le formulaire n'en a pas.</summary>
    public string ChampApplication { get; init; } = "";
}

/// <summary>Source d'un document généré : un document qualité (Document_Entete) OU un document d'exécution (Exec_ControleDocumentStatut).</summary>
public readonly record struct SourceRapport(Guid? DocumentEnteteId, Guid? ExecutionId)
{
    public static SourceRapport Document(Guid documentEnteteId) => new(documentEnteteId, null);
    public static SourceRapport Execution(Guid execControleDocumentStatutId) => new(null, execControleDocumentStatutId);
    public bool EstExecution => ExecutionId.HasValue;
    public override string ToString() => EstExecution ? $"exécution {ExecutionId}" : $"document {DocumentEnteteId}";
}

/// <summary>Formulaire (version) d'un document : identifiant, code de référence et numéro de version.</summary>
/// <remarks>CodeReference : le code en vigueur à la création du document (historique des codes).</remarks>
public sealed record FormulaireDuDocument(Guid Id, string CodeReference, int Version, Guid GroupeId)
{
    public override string ToString() => $"{CodeReference} version {Version}";
}

/// <summary>Document qualité à mettre en forme : son type, sa version de formulaire et son ordre de création.</summary>
public sealed record SourceDocumentQualite(Guid Id, string TypeDocumentCode, string TypeLibelle, string Nom, int Version, string Statut,
    string? PosteCode, FormulaireDuDocument? Formulaire, long OrdreCreation, DateTime CreeLe)
{
    /// <summary>Opération du document (code et libellé), sinon celle de l'OF où il est appliqué (traçabilité d'un OF).</summary>
    public OperationDocument? Operation { get; init; }
}

/// <summary>OF d'un semi-fini (corps, volant) utilisé par un OF d'assemblage.</summary>
public sealed record SemiFiniDeLOf(string NumeroOf, string CodeArticle, string? Designation, string? Nature);

/// <summary>Opération de fabrication (estompage, tronçonnage, usinage, assemblage).</summary>
public sealed record OperationDocument(string Code, string Libelle);

/// <summary>
/// Document d'exécution à mettre en forme et son formulaire : celui des résultats d'assemblage fixé à sa création, sinon
/// celui du document appliqué (plan de vérification de la machine, contrôle au poste, échantillonnage).
/// </summary>
public sealed record SourceDocumentExecution(Guid Id, string TypeExecution, string TypeLibelle, string NumeroOf, string StatutOf,
    bool EstTermine, string? PosteCode, string? MachineCode, FormulaireDuDocument? Formulaire, DateTime? DateExecution, long OrdreCreation,
    Guid? DocumentQualiteId)
{
    /// <summary>Opération de l'OF du document.</summary>
    public OperationDocument? Operation { get; init; }
    /// <summary>
    /// Ordre de la première fiche de l'OF sur la même ligne de formulaire : toutes les fiches d'un OF gardent le modèle en
    /// vigueur à cette première utilisation, même si un modèle change pendant l'OF.
    /// </summary>
    public long OrdreModele { get; init; } = OrdreCreation;
    /// <summary>La fiche porte au moins une réponse (une fiche ouverte puis laissée vide ne compte pas).</summary>
    public bool Remplie { get; init; } = true;
}

/// <summary>Modèle de rapport : type de document mis en forme et rôle des formulaires de ces documents (null : sans formulaire).</summary>
public sealed record ModeleDuType(string Code, string Nom, string? DocumentTypeCode, string? ExecutionTypeCode, string? FormRole);

/// <summary>Rattachement de départ : le formulaire reçoit la version livrée du modèle à son import.</summary>
public sealed record FormulaireInitial(string CodeFormulaire, string CodeModele);

public interface IModeleRapportRepository
{
    Task<List<ReportTemplate>> GetModelesAsync();
    /// <summary>Rôles actifs des formulaires (table FormRole), dans l'ordre d'affichage.</summary>
    Task<List<SopalTrace.Application.Interfaces.RoleFormulaire>> GetRolesFormulaireAsync();
    Task<ReportTemplate?> GetModeleAsync(string code);
    /// <summary>Types sans formulaire (modèle par défaut) qui n'ont encore aucune version.</summary>
    Task<List<string>> GetModelesParDefautSansVersionAsync();

    /// <summary>Version avec son contenu (suivie pour modification du statut).</summary>
    Task<ReportTemplateVersion?> GetVersionAsync(Guid id);
    /// <summary>Versions publiées, avec leur contenu, en lecture seule.</summary>
    Task<ReportTemplateVersion?> GetVersionLectureAsync(Guid id);
    /// <summary>Versions (sans contenu) de ces modèles, les plus récentes d'abord.</summary>
    Task<List<VersionModeleDto>> GetResumesVersionsAsync(IReadOnlyCollection<string> codesModeles);
    Task<VersionModeleDto?> GetVersionResumeAsync(Guid id);
    /// <summary>Dernier numéro des modèles de ce formulaire (de ce type pour un modèle par défaut), 0 s'il n'y en a pas.</summary>
    Task<int> GetDernierNumeroAsync(string codeModele, Guid? groupeFormulaire);
    /// <summary>Version de ce formulaire (modèle par défaut de ce type) déjà envoyée avec exactement ce contenu, ou null.</summary>
    Task<Guid?> GetVersionIdentiqueAsync(string codeModele, Guid? groupeFormulaire, string empreinteContenu);

    /// <summary>Historique des affectations de ces modèles, dans l'ordre de création.</summary>
    Task<List<AffectationModele>> GetAffectationsAsync(IReadOnlyCollection<string> codesModeles);
    /// <summary>Historique détaillé (formulaire, auteur, date) des affectations d'un modèle, les plus récentes d'abord.</summary>
    Task<List<AffectationDto>> GetAffectationsDetailleesAsync(string codeModele);

    /// <summary>Versions de formulaire non archivées de ce rôle et leurs colonnes propres actives.</summary>
    Task<List<ColonnesFormulaire>> GetColonnesFormulairesAsync(string roleFormulaire);
    /// <summary>Une version de formulaire et ses colonnes propres actives, ou null.</summary>
    Task<ColonnesFormulaire?> GetColonnesFormulaireAsync(Guid formulaireId);
    /// <summary>Désignation du formulaire (aperçu d'un modèle), ou null s'il n'existe pas.</summary>
    Task<string?> GetDesignationFormulaireAsync(Guid formulaireId);
    /// <summary>La ligne de formulaire sert à un document ou à une fiche : son identité est figée.</summary>
    Task<bool> FormulaireUtiliseAsync(Guid formulaireId);
    /// <summary>
    /// Nouvelle version d'une ligne de formulaire utilisée (même code, version suivante de la lignée, même structure) : ses
    /// documents génériques en cours et la ligne sont archivés tels quels, les documents recréés à l'identique sur la nouvelle
    /// version (version et nom suivants, motif). À appeler dans <see cref="ExecuterDansTransactionAsync"/>.
    /// </summary>
    Task<(Guid FormulaireId, int Version, int DocumentsRecrees)> NouvelleVersionFormulaireAsync(Guid formulaireId, string matricule, string motif);
    /// <summary>Toutes les versions des formulaires de ces groupes (identifiant commun aux versions), avec l'empreinte de leurs colonnes.</summary>
    Task<List<VersionFormulaire>> GetVersionsFormulairesAsync(IReadOnlyCollection<Guid> groupes);
    /// <summary>Rattachements de départ des modèles livrés.</summary>
    Task<List<FormulaireInitial>> GetFormulairesInitiauxAsync();
    /// <summary>Versions non archivées des formulaires de ce code de référence.</summary>
    Task<List<Guid>> GetVersionsNonArchiveesAsync(string codeReference);
    /// <summary>Un modèle est déjà rattaché à une version de ce formulaire.</summary>
    Task<bool> FormulaireAUnModeleAsync(string codeReference);

    /// <summary>Demandes de modèle (toutes, ou d'un état), les plus récentes d'abord.</summary>
    Task<List<DemandeModeleDto>> GetDemandesAsync(string? statut);
    /// <summary>Demande suivie pour modification, ou null.</summary>
    Task<ReportTemplateRequest?> GetDemandeAsync(long id);
    /// <summary>Demande en attente d'une ligne de formulaire (suivie), ou null.</summary>
    Task<ReportTemplateRequest?> GetDemandeEnAttenteAsync(Guid formulaireId);
    /// <summary>Code de la ligne la plus récente d'un formulaire (nom des fichiers de ses modèles).</summary>
    Task<string?> GetCodeFormulaireDuGroupeAsync(Guid groupe);

    void Ajouter(ReportTemplateVersion version);
    void Ajouter(ReportTemplateAssignment affectation);
    void Ajouter(ReportTemplateRequest demande);
    Task SaveChangesAsync();
    /// <summary>Exécute l'opération dans une transaction (plusieurs enregistrements tout ou rien).</summary>
    Task ExecuterDansTransactionAsync(Func<Task> operation);
}

public interface IDocumentGenereRepository
{
    /// <summary>Document qualité avec tout ce qu'un rapport affiche (sections, lignes, colonnes, référentiels), en lecture seule.</summary>
    Task<DocumentEntete?> GetDocumentCompletAsync(Guid documentEnteteId);
    Task<SourceDocumentQualite?> GetSourceDocumentAsync(Guid documentEnteteId);
    /// <summary>Une ligne de formulaire (code, version, lignée), ou null.</summary>
    Task<FormulaireDuDocument?> GetFormulaireAsync(Guid formulaireId);
    Task<SourceDocumentExecution?> GetSourceExecutionAsync(Guid execControleDocumentStatutId);
    /// <summary>Documents qualité (tous statuts), les plus récents d'abord.</summary>
    Task<List<SourceDocumentQualite>> GetSourcesDocumentsAsync(string? typeDocumentCode, string? recherche, int nombreMaximal);
    /// <summary>Documents d'exécution, les plus récents d'abord ; filtrés par type, OF ou recherche.</summary>
    Task<List<SourceDocumentExecution>> GetSourcesExecutionsAsync(string? typeExecution, string? numeroOf, string? recherche, int nombreMaximal);
    /// <summary>Documents qualité appliqués à un OF : plan suivi par l'OF et documents liés à ses documents d'exécution.</summary>
    Task<List<SourceDocumentQualite>> GetDocumentsAppliquesAsync(string numeroOf);
    /// <summary>Toutes les fiches d'un OF (avec ou sans modèle), sans les clôtures de poste ; un registre par poste.</summary>
    Task<List<SourceDocumentExecution>> GetFichesDeLOfAsync(string numeroOf);
    Task<bool> OfExisteAsync(string numeroOf);
    /// <summary>OF des corps et volants choisis dans le registre de traçabilité de cet OF (lignes non annulées).</summary>
    Task<List<SemiFiniDeLOf>> GetSemiFinisAsync(string numeroOf);

    /// <summary>Modèles des types de documents (code, type, rôle des formulaires).</summary>
    Task<List<ModeleDuType>> GetModelesDesTypesAsync();

    /// <summary>Document déjà généré pour ces données de la source avec cette version du modèle, ou null.</summary>
    Task<GeneratedDocument?> GetGenereAsync(SourceRapport source, string empreinteDonnees, Guid versionModeleId);
    /// <summary>Enregistre le document généré ; faux s'il existait déjà (généré au même instant par une autre demande).</summary>
    Task<bool> AjouterAsync(GeneratedDocument document);

    Task<List<DocumentGenereDto>> GetParSourceAsync(SourceRapport source);
    Task<GeneratedDocument?> GetAsync(Guid id);
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Rapports;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Rapports;
using SopalTrace.Application.Rapports;
using SopalTrace.Application.Rapports.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Services.Rapports;

public interface IGenerationDocumentService
{
    /// <summary>Documents consultables (qualité et exécution), avec le modèle qui met en forme chacun.</summary>
    Task<List<SourceDocumentDto>> GetSourcesAsync(string? categorie, string? type, string? recherche);
    /// <summary>Documents d'un OF : documents qualité appliqués et documents d'exécution.</summary>
    Task<TracabiliteOfDto> GetTracabiliteOfAsync(string numeroOf);
    /// <summary>Le document, son modèle, le modèle actuel s'il diffère, et ses PDF déjà produits.</summary>
    Task<EtatDocumentDto> GetEtatAsync(SourceRapport source);
    /// <summary>
    /// PDF (ou Excel rempli) du document avec son modèle — ou avec le modèle actuel, pour une réimpression : produit à
    /// la demande puis conservé, et réutilisé tant que les données du document et le modèle ne changent pas.
    /// </summary>
    Task<FichierDto> ObtenirAsync(SourceRapport source, bool modeleActuel, bool pdf);
    /// <summary>Fichier Excel du modèle du document (ou du modèle actuel).</summary>
    Task<FichierDto> TelechargerModeleAsync(SourceRapport source, bool modeleActuel);
    /// <summary>PDF ou Excel d'un document déjà produit (historique).</summary>
    Task<FichierDto> TelechargerAsync(Guid documentGenereId, bool pdf);
    /// <summary>
    /// Fiche vierge du document qualité (PDF ou Excel) : la fiche que rempliront les opérateurs, avec le modèle d'une fiche
    /// créée maintenant. Produite à chaque demande, jamais conservée.
    /// </summary>
    Task<FichierDto> FicheViergeAsync(Guid documentEnteteId, bool pdf);
}

/// <summary>
/// Génération des documents à la demande. Un document est mis en forme avec le modèle en vigueur à sa création
/// (<see cref="ResolutionModeleRapport"/>), jamais avec un modèle publié après, sauf réimpression demandée avec le
/// modèle actuel (les champs que le document n'a pas restent vides). Chaque PDF produit est conservé avec la version du
/// modèle et l'empreinte des données : une même donnée n'est produite qu'une fois par version du modèle (unicité
/// garantie par la base).
/// </summary>
public class GenerationDocumentService : IGenerationDocumentService
{
    private const string TypeExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const int NombreMaximalListe = 300;

    private readonly IDocumentGenereRepository _documents;
    private readonly IModeleRapportRepository _modeles;
    private readonly IMoteurModeleExcel _moteur;
    private readonly IConvertisseurPdf _convertisseur;
    private readonly IEnumerable<IDefinitionRapport> _definitions;
    private readonly IEnumerable<IDefinitionRapportExecution> _definitionsExecution;
    private readonly ICurrentUserService _utilisateur;

    public GenerationDocumentService(IDocumentGenereRepository documents, IModeleRapportRepository modeles, IMoteurModeleExcel moteur,
        IConvertisseurPdf convertisseur, IEnumerable<IDefinitionRapport> definitions, IEnumerable<IDefinitionRapportExecution> definitionsExecution,
        ICurrentUserService utilisateur)
    {
        _documents = documents;
        _modeles = modeles;
        _moteur = moteur;
        _convertisseur = convertisseur;
        _definitions = definitions;
        _definitionsExecution = definitionsExecution;
        _utilisateur = utilisateur;
    }

    /// <summary>Modèles des types, affectations et versions de formulaires : tout ce qu'il faut pour résoudre un modèle.</summary>
    private sealed class Contexte
    {
        public required List<ModeleDuType> Modeles { get; init; }
        public required List<AffectationModele> Affectations { get; init; }
        public required List<VersionFormulaire> Formulaires { get; init; }
    }

    // ── Listes ───────────────────────────────────────────────────────────

    public async Task<List<SourceDocumentDto>> GetSourcesAsync(string? categorie, string? type, string? recherche)
    {
        type = string.IsNullOrWhiteSpace(type) ? null : type.Trim();
        recherche = string.IsNullOrWhiteSpace(recherche) ? null : recherche.Trim();
        var documents = categorie == "EXECUTION" ? new List<SourceDocumentQualite>()
            : await _documents.GetSourcesDocumentsAsync(type, recherche, NombreMaximalListe);
        var executions = categorie == "QUALITE" ? new List<SourceDocumentExecution>()
            : await _documents.GetSourcesExecutionsAsync(type, null, recherche, NombreMaximalListe);
        var contexte = await ContexteAsync(documents.Select(d => d.Formulaire).Concat(executions.Select(e => e.Formulaire)));
        return documents.Select(d => VersDto(d, contexte))
            .Concat(executions.Select(e => VersDto(e, contexte)))
            .OrderByDescending(d => d.Date)
            .Take(NombreMaximalListe)
            .ToList();
    }

    public async Task<TracabiliteOfDto> GetTracabiliteOfAsync(string numeroOf)
    {
        numeroOf = numeroOf?.Trim() ?? "";
        if (numeroOf.Length == 0 || !await _documents.OfExisteAsync(numeroOf))
            throw new RessourceIntrouvableException($"L'OF {numeroOf} est inconnu de Sage.");
        var documents = await _documents.GetDocumentsAppliquesAsync(numeroOf);
        // Seules les fiches remplies : une fiche ouverte puis laissée vide n'est pas un document de l'OF.
        var fiches = (await _documents.GetFichesDeLOfAsync(numeroOf)).Where(f => f.Remplie).OrderBy(f => f.OrdreCreation).ToList();
        var contexte = await ContexteAsync(documents.Select(d => d.Formulaire).Concat(fiches.Select(e => e.Formulaire)));

        var resultat = new List<SourceDocumentDto>();
        foreach (var document in documents)
        {
            var dto = VersDto(document, contexte);
            // Document sans PDF propre que remplissent les opérateurs (contrôle au poste, vérification machine,
            // échantillonnage) : ses fiches remplies le représentent ; sans aucune fiche remplie, il manque. Les autres
            // (plan produit fini…) sont disponibles seulement avec leur modèle Excel, comme tout document mis en forme.
            if (!dto.ContratDisponible && _definitionsExecution.Any(d => d.TypeDocumentApplique == document.TypeDocumentCode))
            {
                if (fiches.Any(f => f.DocumentQualiteId == document.Id)) continue;
                dto.Etat = EtatsTracabilite.Manquant;
                dto.RaisonManquant = "Aucune fiche remplie pour ce document dans l'OF.";
            }
            else Etat(dto);
            resultat.Add(dto);
        }
        resultat.AddRange(fiches.Select(f => Etat(VersDto(f, contexte))));
        return new TracabiliteOfDto { NumeroOf = numeroOf, Documents = resultat };
    }

    /// <summary>Disponible : son PDF peut être produit (modèle en place) ; sinon manquant, avec la raison.</summary>
    private static SourceDocumentDto Etat(SourceDocumentDto dto)
    {
        dto.Etat = dto.Modele != null ? EtatsTracabilite.Disponible : EtatsTracabilite.Manquant;
        if (dto.Modele == null)
            dto.RaisonManquant = dto.MessageModeleManquant
                ?? (dto.ContratDisponible ? "Aucun modèle Excel publié pour ce type de document." : "L'Excel de ce type de document n'a pas encore été fourni.");
        return dto;
    }

    public async Task<EtatDocumentDto> GetEtatAsync(SourceRapport source)
    {
        var (dto, actuel) = await SourceAsync(source);
        return new EtatDocumentDto
        {
            Source = dto,
            ModeleActuel = actuel?.VersionId == dto.Modele?.VersionId ? null : actuel,
            Documents = await _documents.GetParSourceAsync(source),
            FicheVierge = source.EstExecution ? null : (await FicheAsync(source.DocumentEnteteId!.Value))?.Fiche
        };
    }

    // ── Fiche vierge d'un document qualité ───────────────────────────────

    public async Task<FichierDto> FicheViergeAsync(Guid documentEnteteId, bool pdf)
    {
        var fiche = await FicheAsync(documentEnteteId)
            ?? throw new RegleMetierException("Ce type de document n'a pas de fiche à remplir par les opérateurs.");
        var modele = fiche.Fiche.Modele
            ?? throw new RegleMetierException(fiche.Fiche.MessageModeleManquant ?? $"Aucun modèle publié pour les fiches « {fiche.Fiche.TypeLibelle} ».");
        var donnees = await fiche.Definition.ChargerViergeAsync(documentEnteteId, fiche.Formulaire)
            ?? throw new RessourceIntrouvableException("Document introuvable.");
        var version = await _modeles.GetVersionLectureAsync(modele.VersionId) ?? throw new RessourceIntrouvableException("Version de modèle introuvable.");
        var classeur = _moteur.Remplir(version.Content, donnees.Donnees);
        return pdf
            ? new FichierDto(await _convertisseur.ConvertirAsync(classeur), "application/pdf", donnees.NomFichier + ".pdf")
            : new FichierDto(classeur, TypeExcel, donnees.NomFichier + ".xlsx");
    }

    /// <summary>Fiche que remplissent les opérateurs avec ce document, son formulaire et son modèle (null : aucune fiche).</summary>
    private async Task<(IDefinitionRapportExecution Definition, FormulaireDuDocument? Formulaire, FicheViergeDto Fiche)?> FicheAsync(Guid documentEnteteId)
    {
        var document = await _documents.GetSourceDocumentAsync(documentEnteteId) ?? throw new RessourceIntrouvableException("Document introuvable.");
        var definition = _definitionsExecution.FirstOrDefault(d => d.TypeDocumentApplique == document.TypeDocumentCode);
        if (definition == null) return null;
        var formulaireId = await definition.FormulaireFicheViergeAsync(documentEnteteId);
        var formulaire = formulaireId is { } id ? await _documents.GetFormulaireAsync(id) : null;
        var contexte = await ContexteAsync(new[] { formulaire });
        var modeles = contexte.Modeles.Where(m => m.ExecutionTypeCode == definition.TypeExecution).ToList();
        var resolution = Resoudre(m => m.ExecutionTypeCode == definition.TypeExecution, formulaire, ResolutionModeleRapport.Maintenant, contexte);
        var fiche = new FicheViergeDto
        {
            TypeExecution = definition.TypeExecution,
            // Nom du modèle qui met en forme la fiche (ex. « Résultat de contrôle au poste (FE0794) »).
            TypeLibelle = (modeles.FirstOrDefault(m => m.Code == resolution?.Modele?.Affectation.CodeModele) ?? modeles.FirstOrDefault())?.Nom
                          ?? definition.TypeExecution,
            Formulaire = formulaire?.ToString(),
            FormulaireId = formulaire?.Id,
            Modele = ModelesDocuments.VersDto(resolution?.Modele)
        };
        if (resolution?.Manque is { } manque)
        {
            fiche.ModeleManquant = ModelesDocuments.Code(manque);
            fiche.MessageModeleManquant = ModelesDocuments.Message(manque, fiche.Formulaire);
        }
        else if (resolution == null)
        {
            fiche.ModeleManquant = ModelesDocuments.Code(ModeleManquant.AucunModele);
            fiche.MessageModeleManquant = "Modèle non disponible : aucun fichier Excel pour cette fiche.";
        }
        return (definition, formulaire, fiche);
    }

    // ── Génération à la demande ──────────────────────────────────────────

    public async Task<FichierDto> ObtenirAsync(SourceRapport source, bool modeleActuel, bool pdf)
    {
        var (dto, actuel) = await SourceAsync(source);
        var modele = (modeleActuel ? actuel : dto.Modele) ?? throw AucunModele(dto);
        var (donnees, nomFichier) = await DonneesAsync(source, dto);
        var empreinte = donnees.Empreinte();

        var genere = await _documents.GetGenereAsync(source, empreinte, modele.VersionId);
        if (genere == null)
        {
            var version = await _modeles.GetVersionLectureAsync(modele.VersionId) ?? throw new RessourceIntrouvableException("Version de modèle introuvable.");
            var classeur = _moteur.Remplir(version.Content, donnees);
            var nouveau = new GeneratedDocument
            {
                Id = Guid.NewGuid(),
                DocumentEnteteId = source.DocumentEnteteId,
                ExecControleDocumentStatutId = source.ExecutionId,
                ReportTemplateVersionId = version.Id,
                SourceDataSha256 = empreinte,
                FileName = nomFichier + (modeleActuel && actuel?.VersionId != dto.Modele?.VersionId ? $"_reimpression_V{version.VersionNumber}" : ""),
                PdfContent = await _convertisseur.ConvertirAsync(classeur),
                WorkbookContent = classeur,
                GeneratedAt = DateTime.Now,
                RequestedBy = _utilisateur.MatriculeRequis
            };
            // Produit au même instant par une autre demande : on reprend celui qui est enregistré.
            genere = await _documents.AjouterAsync(nouveau) ? nouveau : await _documents.GetGenereAsync(source, empreinte, modele.VersionId)
                ?? throw new ConflitConcurrenceException("Ce document vient d'être généré : réessayez.");
        }
        return pdf
            ? new FichierDto(genere.PdfContent, "application/pdf", genere.FileName + ".pdf")
            : new FichierDto(genere.WorkbookContent, TypeExcel, genere.FileName + ".xlsx");
    }

    public async Task<FichierDto> TelechargerModeleAsync(SourceRapport source, bool modeleActuel)
    {
        var (dto, actuel) = await SourceAsync(source);
        var modele = (modeleActuel ? actuel : dto.Modele) ?? throw AucunModele(dto);
        var version = await _modeles.GetVersionLectureAsync(modele.VersionId) ?? throw new RessourceIntrouvableException("Version de modèle introuvable.");
        return new FichierDto(version.Content, TypeExcel, $"{System.IO.Path.GetFileNameWithoutExtension(version.FileName)}_V{version.VersionNumber}.xlsx");
    }

    public async Task<FichierDto> TelechargerAsync(Guid documentGenereId, bool pdf)
    {
        var genere = await _documents.GetAsync(documentGenereId) ?? throw new RessourceIntrouvableException("Document généré introuvable.");
        return pdf
            ? new FichierDto(genere.PdfContent, "application/pdf", genere.FileName + ".pdf")
            : new FichierDto(genere.WorkbookContent, TypeExcel, genere.FileName + ".xlsx");
    }

    // ── Résolution ───────────────────────────────────────────────────────

    /// <summary>Le document (avec son modèle) et le modèle actuel de ce document.</summary>
    private async Task<(SourceDocumentDto Source, ModeleDocumentDto? Actuel)> SourceAsync(SourceRapport source)
    {
        if (source.EstExecution)
        {
            var execution = await _documents.GetSourceExecutionAsync(source.ExecutionId!.Value) ?? throw new RessourceIntrouvableException("Document introuvable.");
            var contexte = await ContexteAsync(new[] { execution.Formulaire });
            return (VersDto(execution, contexte), ModelesDocuments.VersDto(Resoudre(m => m.ExecutionTypeCode == execution.TypeExecution,
                execution.Formulaire, ResolutionModeleRapport.Maintenant, contexte)?.Modele));
        }
        var document = await _documents.GetSourceDocumentAsync(source.DocumentEnteteId!.Value) ?? throw new RessourceIntrouvableException("Document introuvable.");
        var contexteDocument = await ContexteAsync(new[] { document.Formulaire });
        return (VersDto(document, contexteDocument), ModelesDocuments.VersDto(Resoudre(m => m.DocumentTypeCode == document.TypeDocumentCode,
            document.Formulaire, ResolutionModeleRapport.Maintenant, contexteDocument)?.Modele));
    }

    private async Task<Contexte> ContexteAsync(IEnumerable<FormulaireDuDocument?> formulaires)
    {
        var modeles = await _documents.GetModelesDesTypesAsync();
        return new Contexte
        {
            Modeles = modeles,
            Affectations = await _modeles.GetAffectationsAsync(modeles.Select(m => m.Code).ToList()),
            Formulaires = await _modeles.GetVersionsFormulairesAsync(formulaires.Where(f => f != null)
                .Select(f => f!.GroupeId).Distinct().ToList())
        };
    }

    /// <summary>
    /// Modèle d'un document parmi les modèles de son type : celui de son formulaire, ou le modèle par défaut s'il n'a pas
    /// de formulaire ; null si aucun modèle ne met en forme ce type de document.
    /// </summary>
    private static ResolutionModele? Resoudre(Func<ModeleDuType, bool> duType, FormulaireDuDocument? formulaire, long ordre, Contexte contexte)
    {
        var codes = contexte.Modeles.Where(duType).Select(m => m.Code).ToHashSet();
        if (codes.Count == 0) return null;
        var affectations = contexte.Affectations.Where(a => codes.Contains(a.CodeModele)).ToList();
        var version = formulaire == null ? null : contexte.Formulaires.FirstOrDefault(f => f.Id == formulaire.Id);
        return version == null
            ? ResolutionModeleRapport.SansFormulaire(ordre, affectations)
            : ResolutionModeleRapport.PourFormulaire(ordre, version, contexte.Formulaires, affectations);
    }

    private SourceDocumentDto VersDto(SourceDocumentQualite document, Contexte contexte) => AvecModele(new SourceDocumentDto
    {
        DocumentEnteteId = document.Id,
        Categorie = "QUALITE",
        TypeCode = document.TypeDocumentCode,
        TypeLibelle = document.TypeLibelle,
        Nom = document.Nom,
        Version = document.Version,
        Statut = document.Statut,
        Poste = document.PosteCode,
        Operation = document.Operation?.Code,
        OperationLibelle = document.Operation?.Libelle,
        Formulaire = document.Formulaire?.ToString(),
        FormulaireId = document.Formulaire?.Id,
        Date = document.CreeLe,
        ContratDisponible = _definitions.Any(d => d.TypeDocumentCode == document.TypeDocumentCode)
    }, Resoudre(m => m.DocumentTypeCode == document.TypeDocumentCode, document.Formulaire, document.OrdreCreation, contexte));

    private SourceDocumentDto VersDto(SourceDocumentExecution execution, Contexte contexte) => AvecModele(new SourceDocumentDto
    {
        ExecControleDocumentStatutId = execution.Id,
        Categorie = "EXECUTION",
        TypeCode = execution.TypeExecution,
        TypeLibelle = execution.TypeLibelle,
        Nom = string.Join(" · ", new[] { execution.TypeLibelle, execution.MachineCode ?? execution.PosteCode }.Where(p => !string.IsNullOrEmpty(p))),
        Statut = execution.StatutOf == StatutsExecution.Cloture ? "CLOTURE" : execution.EstTermine ? "TERMINE" : "EN_COURS",
        NumeroOf = execution.NumeroOf,
        Poste = execution.PosteCode,
        Machine = execution.MachineCode,
        Operation = execution.Operation?.Code,
        OperationLibelle = execution.Operation?.Libelle,
        Formulaire = execution.Formulaire?.ToString(),
        FormulaireId = execution.Formulaire?.Id,
        Date = execution.DateExecution,
        ContratDisponible = _definitionsExecution.Any(d => d.TypeExecution == execution.TypeExecution)
    }, Resoudre(m => m.ExecutionTypeCode == execution.TypeExecution, execution.Formulaire, execution.OrdreModele, contexte));

    private static SourceDocumentDto AvecModele(SourceDocumentDto dto, ResolutionModele? resolution)
    {
        dto.Modele = ModelesDocuments.VersDto(resolution?.Modele);
        if (resolution?.Manque is { } manque)
        {
            dto.ModeleManquant = ModelesDocuments.Code(manque);
            dto.MessageModeleManquant = ModelesDocuments.Message(manque, dto.Formulaire);
        }
        return dto;
    }

    /// <summary>Données du document mises en forme et nom du fichier.</summary>
    private async Task<(DonneesRapport Donnees, string NomFichier)> DonneesAsync(SourceRapport source, SourceDocumentDto dto)
    {
        if (source.EstExecution)
        {
            var definitionExecution = _definitionsExecution.FirstOrDefault(d => d.TypeExecution == dto.TypeCode) ?? throw SansContrat(dto);
            var execution = await definitionExecution.ChargerAsync(source.ExecutionId!.Value)
                ?? throw new RegleMetierException("Ce document d'exécution n'a pas de PDF propre (registre : le PDF est porté par le premier registre du poste).");
            return (execution.Donnees, execution.NomFichier);
        }
        var definition = _definitions.FirstOrDefault(d => d.TypeDocumentCode == dto.TypeCode) ?? throw SansContrat(dto);
        var document = await _documents.GetDocumentCompletAsync(source.DocumentEnteteId!.Value) ?? throw new RessourceIntrouvableException("Document introuvable.");
        return (definition.Construire(document), definition.NomFichier(document));
    }

    private static RegleMetierException SansContrat(SourceDocumentDto dto) =>
        new($"Les documents « {dto.TypeLibelle} » n'ont pas encore de modèle Excel : fournissez l'Excel vide de ce document pour qu'il soit mis en forme.");

    private static Exception AucunModele(SourceDocumentDto dto) =>
        !dto.ContratDisponible ? SansContrat(dto)
        : new RegleMetierException(dto.MessageModeleManquant ?? $"Aucun modèle publié pour les documents « {dto.TypeLibelle} ».");
}

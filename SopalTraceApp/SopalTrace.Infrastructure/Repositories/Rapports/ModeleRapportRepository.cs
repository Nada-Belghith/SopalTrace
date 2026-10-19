using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SopalTrace.Application.DTOs.Rapports;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Rapports;
using SopalTrace.Application.Rapports;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Infrastructure.Data;

namespace SopalTrace.Infrastructure.Repositories.Rapports;

public class ModeleRapportRepository : IModeleRapportRepository
{
    private readonly SopalTraceDbContext _context;

    public ModeleRapportRepository(SopalTraceDbContext context) => _context = context;

    public Task<List<ReportTemplate>> GetModelesAsync() =>
        _context.ReportTemplates.AsNoTracking().OrderBy(m => m.Name).ToListAsync();

    public Task<List<RoleFormulaire>> GetRolesFormulaireAsync() =>
        _context.FormRoles.AsNoTracking().Where(r => r.Active).OrderBy(r => r.DisplayOrder).ThenBy(r => r.Code)
            .Select(r => new RoleFormulaire(r.Code, r.Label, r.DisplayOrder, r.ScopeByFamily, r.ScopeByWorkstation, r.ScopeByMachine,
                r.ScopeByNature, r.ScopeByValveType, r.Nature))
            .ToListAsync();

    public Task<ReportTemplate?> GetModeleAsync(string code) =>
        _context.ReportTemplates.AsNoTracking().FirstOrDefaultAsync(m => m.Code == code);

    public Task<List<string>> GetModelesParDefautSansVersionAsync() =>
        _context.ReportTemplates.Where(m => m.FormRole == null && !m.ReportTemplateVersions.Any()).Select(m => m.Code).ToListAsync();

    public Task<ReportTemplateVersion?> GetVersionAsync(Guid id) =>
        _context.ReportTemplateVersions.FirstOrDefaultAsync(v => v.Id == id);

    public Task<ReportTemplateVersion?> GetVersionLectureAsync(Guid id) =>
        _context.ReportTemplateVersions.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);

    public Task<List<VersionModeleDto>> GetResumesVersionsAsync(IReadOnlyCollection<string> codesModeles) =>
        ResumesAsync(_context.ReportTemplateVersions.AsNoTracking().Where(v => codesModeles.Contains(v.ReportTemplateCode)));

    public async Task<VersionModeleDto?> GetVersionResumeAsync(Guid id) =>
        (await ResumesAsync(_context.ReportTemplateVersions.AsNoTracking().Where(v => v.Id == id))).FirstOrDefault();

    public async Task<int> GetDernierNumeroAsync(string codeModele, Guid? groupeFormulaire) =>
        await DuProprietaire(codeModele, groupeFormulaire).MaxAsync(v => (int?)v.VersionNumber) ?? 0;

    public Task<Guid?> GetVersionIdentiqueAsync(string codeModele, Guid? groupeFormulaire, string empreinteContenu) =>
        DuProprietaire(codeModele, groupeFormulaire)
            .Where(v => v.ReportTemplateCode == codeModele && v.ContentSha256 == empreinteContenu)
            .Select(v => (Guid?)v.Id)
            .FirstOrDefaultAsync();

    /// <summary>Versions d'un formulaire (numérotées dans le formulaire), ou modèles par défaut d'un type sans formulaire.</summary>
    private IQueryable<ReportTemplateVersion> DuProprietaire(string codeModele, Guid? groupeFormulaire) => groupeFormulaire is { } groupe
        ? _context.ReportTemplateVersions.Where(v => v.FormGroupId == groupe)
        : _context.ReportTemplateVersions.Where(v => v.ReportTemplateCode == codeModele && v.FormGroupId == null);

    public Task<List<AffectationModele>> GetAffectationsAsync(IReadOnlyCollection<string> codesModeles) =>
        _context.ReportTemplateAssignments.AsNoTracking()
            .Where(a => codesModeles.Contains(a.ReportTemplateVersion.ReportTemplateCode))
            .OrderBy(a => a.CreationOrder)
            .Select(a => new AffectationModele(a.ReportTemplateVersionId, a.ReportTemplateVersion.ReportTemplateCode,
                a.ReportTemplateVersion.VersionNumber, a.Kind, a.FormId, a.CreationOrder))
            .ToListAsync();

    public Task<List<AffectationDto>> GetAffectationsDetailleesAsync(string codeModele) =>
        _context.ReportTemplateAssignments.AsNoTracking()
            .Where(a => a.ReportTemplateVersion.ReportTemplateCode == codeModele)
            .OrderByDescending(a => a.CreationOrder)
            .Select(a => new AffectationDto
            {
                Id = a.Id,
                VersionId = a.ReportTemplateVersionId,
                NumeroVersion = a.ReportTemplateVersion.VersionNumber,
                NomModele = a.ReportTemplateVersion.Name,
                Nature = a.Kind,
                FormulaireId = a.FormId,
                GroupeFormulaire = a.Form != null ? a.Form.VersionGroupId : null,
                CodeFormulaire = a.Form != null ? a.Form.CodeReference : null,
                VersionFormulaire = a.Form != null ? a.Form.Version : null,
                Commentaire = a.Comment,
                AffectePar = a.AssignedBy,
                AffecteParNom = a.AssignedByNavigation != null ? a.AssignedByNavigation.Intusr0 : null,
                AffecteLe = a.AssignedAt
            })
            .ToListAsync();

    public Task<List<ColonnesFormulaire>> GetColonnesFormulairesAsync(string roleFormulaire) =>
        ColonnesAsync(_context.RefFormulaires.AsNoTracking().Where(f => f.Statut != StatutsPlan.Archive && f.Role == roleFormulaire));

    public Task<string?> GetDesignationFormulaireAsync(Guid formulaireId) =>
        _context.RefFormulaires.AsNoTracking().Where(f => f.Id == formulaireId).Select(f => f.Designation).FirstOrDefaultAsync();

    public async Task<ColonnesFormulaire?> GetColonnesFormulaireAsync(Guid formulaireId) =>
        (await ColonnesAsync(_context.RefFormulaires.AsNoTracking().Where(f => f.Id == formulaireId))).FirstOrDefault();

    public async Task<List<VersionFormulaire>> GetVersionsFormulairesAsync(IReadOnlyCollection<Guid> groupes)
    {
        if (groupes.Count == 0) return new List<VersionFormulaire>();
        var formulaires = await _context.RefFormulaires.AsNoTracking()
            .AsSplitQuery()
            .Where(f => groupes.Contains(f.VersionGroupId))
            .Include(f => f.RefFormulaireColonneDefs).ThenInclude(c => c.ColumnAllowedValues)
            .ToListAsync();
        // Empreinte des colonnes actives (clé, libellé, type, position, valeurs proposées) : mêmes colonnes, même empreinte.
        return formulaires
            .Select(f => new VersionFormulaire(f.Id, f.CodeReference, f.Version, ColonneJsonMapper.SignatureColonnes(f.RefFormulaireColonneDefs), f.VersionGroupId, f.CreationOrder))
            .ToList();
    }

    public Task<List<FormulaireInitial>> GetFormulairesInitiauxAsync() =>
        _context.ReportTemplateInitialForms.AsNoTracking()
            .OrderBy(i => i.FormCodeReference)
            .Select(i => new FormulaireInitial(i.FormCodeReference, i.ReportTemplateCode))
            .ToListAsync();

    public Task<List<Guid>> GetVersionsNonArchiveesAsync(string codeReference) =>
        _context.RefFormulaires.AsNoTracking()
            .Where(f => f.CodeReference == codeReference && f.Statut != StatutsPlan.Archive)
            .Select(f => f.Id)
            .ToListAsync();

    public Task<bool> FormulaireAUnModeleAsync(string codeReference) =>
        _context.ReportTemplateAssignments.AnyAsync(a => a.Form != null
            && _context.RefFormulaires.Any(f => f.CodeReference == codeReference && f.VersionGroupId == a.Form.VersionGroupId));

    public async Task<List<DemandeModeleDto>> GetDemandesAsync(string? statut)
    {
        var lignes = await _context.ReportTemplateRequests.AsNoTracking()
            .Where(r => statut == null || r.Status == statut)
            .OrderByDescending(r => r.RequestedAt).ThenByDescending(r => r.Id)
            .Select(r => new
            {
                r.Id, r.FormId, r.Form.VersionGroupId, r.Form.CodeReference, r.Form.Version, r.Form.Role,
                RoleLibelle = r.Form.RoleNavigation != null ? r.Form.RoleNavigation.Label : "",
                Nature = r.Form.RoleNavigation != null ? r.Form.RoleNavigation.Nature : "",
                r.Form.FamilleProduitFiniCode, r.Form.PosteCode, r.Form.MachineCode, r.Form.NatureArticleCode, r.Form.TypeRobinetCode,
                r.Origin, r.Reason, r.VersionChange, r.Status, r.RequestedBy, r.RequestedAt, r.HandledBy, r.HandledAt,
                DemandeParNom = _context.Autilis.Where(u => u.Usr0 == r.RequestedBy).Select(u => u.Intusr0).FirstOrDefault(),
                TraiteParNom = _context.Autilis.Where(u => u.Usr0 == r.HandledBy).Select(u => u.Intusr0).FirstOrDefault(),
                VersionId = r.Assignment != null ? (Guid?)r.Assignment.ReportTemplateVersionId : null,
                Numero = r.Assignment != null ? (int?)r.Assignment.ReportTemplateVersion.VersionNumber : null,
                NomModele = r.Assignment != null ? r.Assignment.ReportTemplateVersion.Name : null
            })
            .ToListAsync();
        return lignes.Select(r => new DemandeModeleDto
        {
            Id = r.Id,
            FormulaireId = r.FormId,
            GroupeFormulaire = r.VersionGroupId,
            CodeFormulaire = r.CodeReference,
            VersionFormulaire = r.Version,
            Role = r.Role,
            RoleLibelle = r.RoleLibelle,
            Nature = r.Nature,
            ChampApplication = string.Join(" · ", new[]
            {
                r.FamilleProduitFiniCode == null ? null : $"famille {r.FamilleProduitFiniCode}",
                r.TypeRobinetCode == null || r.FamilleProduitFiniCode != null ? null : $"type {r.TypeRobinetCode}",
                r.PosteCode == null ? null : $"poste {r.PosteCode}",
                r.MachineCode == null ? null : $"machine {r.MachineCode}",
                r.NatureArticleCode == null ? null : $"nature {r.NatureArticleCode}"
            }.Where(t => t != null)),
            Origine = r.Origin,
            Motif = r.Reason,
            ChangementVersion = r.VersionChange,
            Statut = r.Status,
            DemandePar = r.RequestedBy,
            DemandeParNom = r.DemandeParNom,
            DemandeLe = r.RequestedAt,
            TraitePar = r.HandledBy,
            TraiteParNom = r.TraiteParNom,
            TraiteLe = r.HandledAt,
            VersionModeleId = r.VersionId,
            NumeroModele = r.Numero,
            NomModele = r.NomModele
        }).ToList();
    }

    public Task<ReportTemplateRequest?> GetDemandeAsync(long id) => _context.ReportTemplateRequests.FirstOrDefaultAsync(r => r.Id == id);

    public Task<ReportTemplateRequest?> GetDemandeEnAttenteAsync(Guid formulaireId) =>
        _context.ReportTemplateRequests.FirstOrDefaultAsync(r => r.FormId == formulaireId && r.Status == DemandesModele.EnAttente);

    public Task<string?> GetCodeFormulaireDuGroupeAsync(Guid groupe) =>
        _context.RefFormulaires.AsNoTracking().Where(f => f.VersionGroupId == groupe).OrderByDescending(f => f.CreationOrder)
            .Select(f => f.CodeReference).FirstOrDefaultAsync();

    public void Ajouter(ReportTemplateRequest demande) => _context.ReportTemplateRequests.Add(demande);

    public void Ajouter(ReportTemplateVersion version) => _context.ReportTemplateVersions.Add(version);

    public void Ajouter(ReportTemplateAssignment affectation) => _context.ReportTemplateAssignments.Add(affectation);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    public Task<bool> FormulaireUtiliseAsync(Guid formulaireId) => RemplacementFormulaire.EstUtiliseAsync(_context, formulaireId);

    public async Task<(Guid FormulaireId, int Version, int DocumentsRecrees)> NouvelleVersionFormulaireAsync(Guid formulaireId, string matricule,
        string motif)
    {
        var groupe = await _context.RefFormulaires.Where(f => f.Id == formulaireId).Select(f => f.VersionGroupId).FirstAsync();
        var version = await _context.RefFormulaires.Where(f => f.VersionGroupId == groupe).MaxAsync(f => f.Version) + 1;
        var (nouveau, recrees) = await RemplacementFormulaire.RemplacerAsync(_context, formulaireId,
            ancien => RemplacementFormulaire.Copie(ancien, ancien.CodeReference, version, matricule), matricule, _ => motif);
        return (nouveau.Id, nouveau.Version, recrees);
    }

    public async Task ExecuterDansTransactionAsync(Func<Task> operation)
    {
        var strategie = _context.Database.CreateExecutionStrategy();
        await strategie.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            await operation();
            await transaction.CommitAsync();
        });
    }

    private static async Task<List<ColonnesFormulaire>> ColonnesAsync(IQueryable<RefFormulaire> formulaires)
    {
        var lignes = await formulaires
            .Select(f => new
            {
                f.Id,
                f.CodeReference,
                f.Version,
                f.Statut,
                f.Role,
                f.VersionGroupId,
                f.FamilleProduitFiniCode,
                f.PosteCode,
                f.MachineCode,
                f.NatureArticleCode,
                f.TypeRobinetCode,
                Colonnes = f.RefFormulaireColonneDefs.Where(c => c.Actif).Select(c => new { c.CleColonne, c.LabelAffiche }).ToList()
            })
            .OrderBy(f => f.CodeReference).ThenBy(f => f.Version)
            .ToListAsync();
        return lignes
            .Select(f => new ColonnesFormulaire(f.Id, f.CodeReference, f.Version, f.Statut, f.Role,
                f.Colonnes.GroupBy(c => c.CleColonne).ToDictionary(c => c.Key, c => c.First().LabelAffiche, StringComparer.Ordinal))
            {
                GroupeId = f.VersionGroupId,
                ChampApplication = string.Join(" · ", new[]
                {
                    f.FamilleProduitFiniCode == null ? null : $"famille {f.FamilleProduitFiniCode}",
                    // Une famille porte son type de robinet : il n'est cité que sans famille.
                    f.TypeRobinetCode == null || f.FamilleProduitFiniCode != null ? null : $"type {f.TypeRobinetCode}",
                    f.PosteCode == null ? null : $"poste {f.PosteCode}",
                    f.MachineCode == null ? null : $"machine {f.MachineCode}",
                    f.NatureArticleCode == null ? null : $"nature {f.NatureArticleCode}"
                }.Where(t => t != null))
            })
            .ToList();
    }

    private static async Task<List<VersionModeleDto>> ResumesAsync(IQueryable<ReportTemplateVersion> versions) => (await versions
        .OrderByDescending(v => v.VersionNumber)
        .Select(v => new
        {
            v.ReportTemplateCode,
            v.Id,
            v.FormGroupId,
            v.Name,
            v.VersionNumber,
            v.FileName,
            v.Status,
            v.Comment,
            v.ValidationWarnings,
            v.ContentSha256,
            v.UploadedBy,
            EnvoyeParNom = v.UploadedByNavigation != null ? v.UploadedByNavigation.Intusr0 : null,
            v.UploadedAt,
            v.PublishedBy,
            PublieParNom = v.PublishedByNavigation != null ? v.PublishedByNavigation.Intusr0 : null,
            v.PublishedAt
        })
        .ToListAsync())
        .Select(v => new VersionModeleDto
        {
            Id = v.Id,
            CodeModele = v.ReportTemplateCode,
            GroupeFormulaire = v.FormGroupId,
            Nom = v.Name,
            Numero = v.VersionNumber,
            NomFichier = v.FileName,
            Statut = v.Status,
            Commentaire = v.Comment,
            Avertissements = string.IsNullOrEmpty(v.ValidationWarnings) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v.ValidationWarnings) ?? new List<string>(),
            EmpreinteSha256 = v.ContentSha256,
            EnvoyePar = v.UploadedBy,
            EnvoyeParNom = v.EnvoyeParNom,
            EnvoyeLe = v.UploadedAt,
            PubliePar = v.PublishedBy,
            PublieParNom = v.PublieParNom,
            PublieLe = v.PublishedAt
        })
        .ToList();
}

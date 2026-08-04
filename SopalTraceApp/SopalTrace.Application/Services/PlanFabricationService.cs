using SopalTrace.Application.DTOs.QualityPlans.Fabrication;
using SopalTrace.Application.DTOs.QualityPlans.Documents;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Mappers;
using SopalTrace.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using SopalTrace.Application.Helpers;

namespace SopalTrace.Application.Services;

public class PlanFabricationService : IPlanFabricationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFormulaireStructureService _formulaireStructureService;
    private readonly IFrequencyParserService _frequencyParserService;
    private readonly IEmailService _emailService;

    public PlanFabricationService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IFormulaireStructureService formulaireStructureService,
        IFrequencyParserService frequencyParserService,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _formulaireStructureService = formulaireStructureService;
        _frequencyParserService = frequencyParserService;
        _emailService = emailService;
    }

    public async Task<PlanFabricationEnteteDto?> GetPlanByIdAsync(Guid id)
    {
        var plan = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (plan == null) return null;

        string? configJson = null;
        if (plan.FormulaireId.HasValue)
        {
            var activeCols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(plan.FormulaireId.Value);
            if (activeCols != null)
            {
                configJson = ColonneJsonMapper.Serialize(activeCols);
            }
        }

        return PlanFabricationMapper.ToDto(plan, configJson);
    }

    public async Task<IReadOnlyList<PlanFabricationEnteteDto>> GetPlansByFiltersAsync(string? natureComposantCode = null, string? operationCode = null, string? familleProduitCode = null, string? statut = null, string? codeArticleSageVersionne = null)
    {
        var plans = await _unitOfWork.PlanFabricationEnteteRepository.GetByFiltersAsync(operationCode);
        
        if (!string.IsNullOrWhiteSpace(statut))
        {
            plans = plans.Where(p => p.Statut == statut);
        }

        if (!string.IsNullOrWhiteSpace(codeArticleSageVersionne))
        {
            plans = plans.Where(p => p.CodeArticleSageVersionne == codeArticleSageVersionne);
        }

        return plans.Select(PlanFabricationMapper.ToSummaryDto).ToList();
    }

    public async Task<Guid> CreatePlanAsync(CreatePlanFabricationRequestDto request)
    {
        var (user, currentUserEmail) = await ResolveCurrentUserAsync();

        var codeArticleSageVersionne = !string.IsNullOrWhiteSpace(request.CodeArticleSageVersionne) ? request.CodeArticleSageVersionne : request.Nom;
        var lastDotIndex = codeArticleSageVersionne?.LastIndexOf('.') ?? -1;
        var baseNom = lastDotIndex > 0 ? codeArticleSageVersionne!.Substring(0, lastDotIndex) : (codeArticleSageVersionne ?? "");

        var existingDocs = await _unitOfWork.PlanFabricationEnteteRepository.GetByFiltersAsync(request.OperationCode);
        var existingDoc = existingDocs.Where(d => d.CodeArticleSageVersionne == codeArticleSageVersionne)
                                      .OrderByDescending(d => d.Version)
                                      .FirstOrDefault();

        bool forceArchive = request.VersionInitiale.HasValue && request.VersionInitiale.Value != (existingDoc?.Version ?? -1);

        if (existingDoc != null && existingDoc.Statut == "BROUILLON" && !forceArchive)
        {
            await DeleteDraftIfExistsAsync(existingDoc);
        }

        if (request.Statut != "BROUILLON")
        {
            ArchiveActivePlans(existingDocs, baseNom);
        }

        int finalVersion = CalculateNextVersion(existingDoc, request.VersionInitiale, forceArchive);
        string finalStatus = ResolvePlanStatus(request.Statut, existingDoc, forceArchive);
        Guid? formulaireId = await GetFormulaireIdByRoleAsync("EN_COURS_DE_FABRICATION");

        var plan = new PlanFabricationEntete
        {
            Id = Guid.NewGuid(),
            CodeArticleSageVersionne = codeArticleSageVersionne ?? string.Empty,
            Nom = codeArticleSageVersionne ?? string.Empty,
            Designation = request.Designation,
            Version = finalVersion,
            Statut = finalStatus,
            OperationCode = request.OperationCode,
            FormulaireId = formulaireId,
            LegendeMoyens = request.LegendeMoyens,
            CreePar = user,
            CreeLe = DateTime.UtcNow,
            ModeleSourceId = request.ModeleSourceId,
            PlanFabricationSections = new List<PlanFabricationSection>()
        };

        await AddSectionsAndLinesAsync(plan, request.Sections, formulaireId);

        await _unitOfWork.PlanFabricationEnteteRepository.AddAsync(plan);
        await _unitOfWork.CommitAsync();

        await ResolveMissingPlanAlertsAsync(plan.CodeArticleSageVersionne, user, currentUserEmail);

        return plan.Id;
    }

    public async Task<Guid> CreateNewVersionAsync(NouvelleVersionPlanFabricationRequestDto request)
    {
        var existingPlan = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(request.AncienId, includeRelations: true);
        if (existingPlan == null) throw new Exception("Plan introuvable");

        var createReq = PlanFabricationMapper.ToCreatePlanRequest(existingPlan, request);
        return await CreatePlanAsync(createReq);
    }

    public async Task<bool> UpdatePlanAsync(Guid id, UpdatePlanFabricationRequestDto request)
    {
        var plan = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (plan == null) return false;

        UpdatePlanEnteteProperties(plan, request);

        Guid? formulaireId = await GetFormulaireIdByRoleAsync("EN_COURS_DE_FABRICATION");
        if (formulaireId.HasValue)
        {
            plan.FormulaireId = formulaireId;
        }

        if (request.Sections != null)
        {
            foreach (var s in request.Sections)
            {
                if (!s.PeriodiciteId.HasValue && !string.IsNullOrEmpty(s.LibelleSection))
                {
                    s.PeriodiciteId = await _frequencyParserService.ResolveOrCreatePeriodiciteFromTextAsync(s.LibelleSection);
                }
            }

            SopalTrace.Application.Utilities.SectionUpdateHelper.UpdateSections(
                plan.PlanFabricationSections,
                request.Sections,
                sec => _unitOfWork.PlanFabricationEnteteRepository.RemoveSection(sec),
                lig => _unitOfWork.PlanFabricationEnteteRepository.RemoveLigne(lig),
                dto => dto.Id,
                dto => dto.Id,
                sec => sec.Id,
                lig => lig.Id,
                dto => PlanFabricationMapper.CreateSection(dto, plan.Id),
                (sec, dto) => PlanFabricationMapper.UpdateSection(sec, dto),
                sec => sec.PlanFabricationLignes,
                dto => dto.Lignes,
                (dto, sec) => PlanFabricationMapper.CreateLigne(dto, plan.Id, sec.Id),
                (lig, dto) => 
                {
                    PlanFabricationMapper.UpdateLigne(lig, dto);
                    PlanFabricationMapper.MergerExtraColonnes(lig, dto.ExtraColonnes ?? new(), _unitOfWork);
                }
            );
        }

        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<bool> DeletePlanAsync(Guid id)
    {
        var plan = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(id, includeRelations: true);
        if (plan == null) return false;

        await _unitOfWork.PlanFabricationEnteteRepository.DeleteAsync(plan);
        await _unitOfWork.CommitAsync();
        return true;
    }

    public async Task<Guid> UpgradeArchivedPlanAsync(Guid archiveId)
    {
        var plan = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(archiveId, includeRelations: true);
        if (plan == null) throw new Exception("Plan introuvable");

        var maxVersion = await _unitOfWork.PlanFabricationEnteteRepository.GetLatestVersionAsync(plan.CodeArticleSageVersionne, plan.OperationCode);
        var createReq = PlanFabricationMapper.ToCreatePlanRequest(plan, maxVersion + 1);

        return await CreatePlanAsync(createReq);
    }

    public async Task ArchiveDocumentsByFormulaireAsync(Guid formulaireId)
    {
        var plansFabrication = await _unitOfWork.PlanFabricationEnteteRepository.GetByFormulaireIdAsync(formulaireId);
        foreach (var plan in plansFabrication.Where(p => p.Statut == "ACTIF"))
        {
            plan.Statut = "ARCHIVE";
        }
        await _unitOfWork.CommitAsync();
    }

    private async Task<(string User, string? Email)> ResolveCurrentUserAsync()
    {
        var user = _currentUserService.UserInfo ?? "";
        string? currentUserEmail = null;
        var currentUserMatricule = _currentUserService.Matricule;

        if (!string.IsNullOrEmpty(currentUserMatricule))
        {
            var dbUser = await _unitOfWork.UserRepository.GetUserByMatriculeAsync(currentUserMatricule);
            if (dbUser != null)
            {
                user = $"{dbUser.Matricule} - {dbUser.NomComplet}";
                currentUserEmail = dbUser.Email;
            }
        }

        return (user, currentUserEmail);
    }

    private void UpdatePlanEnteteProperties(PlanFabricationEntete plan, UpdatePlanFabricationRequestDto request)
    {
        plan.Remarques = request.Remarques;
        plan.LegendeMoyens = request.LegendeMoyens;
        plan.OperationCode = request.OperationCode ?? plan.OperationCode;
        if (!string.IsNullOrWhiteSpace(request.CodeArticleSageVersionne))
        {
            plan.CodeArticleSageVersionne = request.CodeArticleSageVersionne;
            plan.Nom = request.CodeArticleSageVersionne;
        }
        else if (!string.IsNullOrWhiteSpace(request.Nom))
        {
            plan.CodeArticleSageVersionne = request.Nom;
            plan.Nom = request.Nom;
        }
        plan.ModifiePar = _currentUserService.UserInfo ?? "";
        plan.ModifieLe = DateTime.UtcNow;
    }

    private async Task DeleteDraftIfExistsAsync(PlanFabricationEntete existingDraft)
    {
        var fullDraft = await _unitOfWork.PlanFabricationEnteteRepository.GetByIdAsync(existingDraft.Id, includeRelations: true);
        if (fullDraft != null)
        {
            await _unitOfWork.PlanFabricationEnteteRepository.DeleteAsync(fullDraft);
        }
    }

    private void ArchiveActivePlans(IEnumerable<PlanFabricationEntete> existingDocs, string baseNom)
    {
        var activeDocs = existingDocs.Where(d => 
            (d.CodeArticleSageVersionne == baseNom || (d.CodeArticleSageVersionne != null && d.CodeArticleSageVersionne.StartsWith(baseNom + "."))) 
            && d.Statut == "ACTIF"
        ).ToList();
        
        foreach (var act in activeDocs)
        {
            act.Statut = "ARCHIVE";
        }
    }

    private int CalculateNextVersion(PlanFabricationEntete? existingDoc, int? requestedVersion, bool forceArchive)
    {
        if (existingDoc == null) return requestedVersion ?? 0;

        if (existingDoc.Statut == "BROUILLON" && !forceArchive)
        {
            return existingDoc.Version;
        }

        var maxVersion = existingDoc.Version;
        return (requestedVersion.HasValue && requestedVersion.Value > maxVersion) ? requestedVersion.Value : (maxVersion + 1);
    }

    private string ResolvePlanStatus(string? requestedStatus, PlanFabricationEntete? existingDoc, bool forceArchive)
    {
        if (!string.IsNullOrWhiteSpace(requestedStatus))
        {
            return requestedStatus;
        }

        return (existingDoc != null && existingDoc.Statut == "BROUILLON" && !forceArchive) ? "BROUILLON" : "ACTIF";
    }

    private async Task<Guid?> GetFormulaireIdByRoleAsync(string role)
    {
        var formStruct = await _formulaireStructureService.GetFormulaireByRoleAsync(role);
        return formStruct?.Id;
    }

    private async Task AddSectionsAndLinesAsync(PlanFabricationEntete plan, IEnumerable<CreatePlanFabricationSectionDto>? requestSections, Guid? formulaireId)
    {
        if (!formulaireId.HasValue || requestSections == null) return;

        var form = await _unitOfWork.RefFormulaireRepository.GetByIdAsync(formulaireId.Value);
        if (form == null) return;

        var activeCols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(form.Id);

        foreach (var s in requestSections)
        {
            var periodiciteId = s.PeriodiciteId ?? (await _frequencyParserService.ResolveOrCreatePeriodiciteFromTextAsync(s.LibelleSection ?? string.Empty));
            var planSec = PlanFabricationMapper.ToSectionEntity(s, plan, periodiciteId, activeCols);

            if (plan.PlanFabricationSections == null)
            {
                plan.PlanFabricationSections = new List<PlanFabricationSection>();
            }
            plan.PlanFabricationSections.Add(planSec);
        }
    }


    private async Task ResolveMissingPlanAlertsAsync(string codeArticleSageVersionne, string user, string? currentUserEmail)
    {
        try
        {
            var alertes = await _unitOfWork.AlerteRepository.GetNonResoluesParArticleAsync(codeArticleSageVersionne ?? "");
            if (!alertes.Any()) return;

            foreach (var alerte in alertes)
            {
                alerte.EstResolu = true;
                alerte.DateResolution = DateTime.UtcNow;
                await _unitOfWork.AlerteRepository.UpdateAsync(alerte);

                var emails = alerte.Destinataires.Split(',')
                                    .Where(d => !string.IsNullOrWhiteSpace(d))
                                    .Select(d => d.Trim())
                                    .Where(d => string.IsNullOrEmpty(currentUserEmail) || !d.Equals(currentUserEmail, StringComparison.OrdinalIgnoreCase))
                                    .Distinct()
                                    .ToList();

                var sujet = $"[Résolu] Le plan manquant a été créé pour {codeArticleSageVersionne}";
                var corps = $"<p>Bonjour,</p><p>Le plan de fabrication pour l'article <b>{codeArticleSageVersionne}</b> a été créé par <b>{user}</b>.</p><p>L'alerte associée a été automatiquement marquée comme résolue.</p>";

                foreach (var email in emails)
                {
                    try { await _emailService.EnvoyerAsync(email, sujet, corps, isHtml: true); } catch { }
                }

                try
                {
                    if (!string.IsNullOrWhiteSpace(alerte.DonneesContexte))
                    {
                        var contexte = System.Text.Json.JsonSerializer.Deserialize<SopalTrace.Application.Alertes.PlanManquantContexte>(alerte.DonneesContexte);
                        if (contexte != null && !string.IsNullOrWhiteSpace(contexte.EmailOperateur))
                        {
                            var sujetOp = $"Le plan {codeArticleSageVersionne} est maintenant disponible";
                            var corpsOp = $"<p>Bonjour {contexte.NomOperateur},</p><p>Le plan que vous avez signalé manquant pour l'article <b>{codeArticleSageVersionne}</b> a été créé.</p><p>Vous pouvez dès à présent retourner sur la plateforme et démarrer votre Ordre de Fabrication.</p>";
                            await _emailService.EnvoyerAsync(contexte.EmailOperateur, sujetOp, corpsOp, isHtml: true);
                        }
                    }
                }
                catch { }
            }
            await _unitOfWork.CommitAsync();
        }
        catch { }
    }
}

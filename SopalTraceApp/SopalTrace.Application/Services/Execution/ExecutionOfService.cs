using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services.Execution;

public class ExecutionOfService : IExecutionOfService
{
    private readonly IOperateurRepository _operateurRepository;
    private readonly IOccurrenceService _occurrenceService;
    private readonly IOccurrenceRepository _occurrenceRepository;

    public ExecutionOfService(IOperateurRepository operateurRepository, IOccurrenceService occurrenceService, IOccurrenceRepository occurrenceRepository)
    {
        _operateurRepository = operateurRepository;
        _occurrenceService = occurrenceService;
        _occurrenceRepository = occurrenceRepository;
    }

    public async Task<ExecControleOfDto> DemarrerOfAsync(DemarrerOfRequest request)
    {
        var of = await _operateurRepository.GetOfAsync(request.NumeroOf);
        if (of == null) throw new Exception("OF introuvable");

        if (!await _operateurRepository.CanStartOperationAsync(request.NumeroOf, request.OperationCode))
        {
            throw new Exception("Vous ne pouvez pas démarrer cette opération car l'opération précédente dans la gamme n'a pas encore commencé.");
        }

        var planFab = await _operateurRepository.GetPlanActifAsync(of.CodeArticle, request.OperationCode);
        if (planFab == null) throw new Exception("Aucun plan de fabrication actif pour cet article");

        if (request.OperationCode == "TRONC" || request.OperationCode == "TRN")
        {
            if (request.Longueur.HasValue || request.Diametre.HasValue)
            {
                var lignes = planFab.PlanFabricationSections.SelectMany(s => s.PlanFabricationLignes).ToList();
                
                if (request.Longueur.HasValue)
                {
                    var ligneLongueur = lignes.FirstOrDefault(l => l.LibelleAffiche != null && l.LibelleAffiche.Contains("longueur", StringComparison.OrdinalIgnoreCase));
                    if (ligneLongueur != null)
                    {
                        var baseLibelle = ligneLongueur.LibelleAffiche!.Split('=')[0];
                        ligneLongueur.LibelleAffiche = $"{baseLibelle}= {request.Longueur.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                    }
                }

                if (request.Diametre.HasValue)
                {
                    var ligneDiametre = lignes.FirstOrDefault(l => l.LibelleAffiche != null && 
                        (l.LibelleAffiche.Contains("Diametre", StringComparison.OrdinalIgnoreCase) || 
                         l.LibelleAffiche.Contains("Diamètre", StringComparison.OrdinalIgnoreCase)));
                    if (ligneDiametre != null)
                    {
                        var baseLibelle = ligneDiametre.LibelleAffiche!.Split('=')[0];
                        ligneDiametre.LibelleAffiche = $"{baseLibelle}= {request.Diametre.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                    }
                }
            }
        }

        bool aDesControlesReglage = planFab.PlanFabricationSections.Any(s => 
            (s.TypeSection?.Code == "REGLAGE" || s.TypeSection?.Code == "REGLAGE_PROD") 
            && s.PlanFabricationLignes.Any());

        var execOf = new ExecControleOf
        {
            NumeroOf = request.NumeroOf,
            OperationCode = request.OperationCode,
            PlanSourceId = planFab.Id,
            TypeOf = "FAB",
            Statut = aDesControlesReglage ? "REGLAGE" : "EN_COURS",
            EstEnReglage = aDesControlesReglage,
            DateDebut = DateTime.Now,
            DateFin = aDesControlesReglage ? DateTime.Now : null
        };

        if (request.OperationCode == "USI" || request.OperationCode == "TRN" || request.OperationCode == "ESTOMP" || request.OperationCode == "TRONC")
        {
            execOf.MachineCodePrevu = string.IsNullOrEmpty(planFab.MachineDefautCode) ? request.MachineCode : planFab.MachineDefautCode;
            execOf.MachineCode = request.MachineCode;
        }
        else if (request.OperationCode == "ASS")
        {
            var inputPostes = request.PosteCode ?? request.MachineCode ?? "";
            execOf.PosteCodePrevu = inputPostes;
            execOf.PosteCode = inputPostes;

            var postes = inputPostes.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(p => p.Trim())
                                    .Distinct()
                                    .ToList();

            foreach (var poste in postes)
            {
                execOf.ExecControleOfPostes.Add(new ExecControleOfPoste
                {
                    PosteCode = poste
                });
            }
        }

        _operateurRepository.AddExecControleOf(execOf);
        await _operateurRepository.SaveChangesAsync();

        if (aDesControlesReglage)
        {
            await _occurrenceService.GenererOccurrencesReglageCoursAsync(execOf.Id);
        }

        await _occurrenceService.GenererOccurrencesInitialesAsync(execOf.Id);

        return await MapToDto(execOf.Id);
    }

    public async Task<bool> MettreEnReglageAsync(Guid execControleOfId)
    {
        var execOf = await _operateurRepository.GetExecOfWithIntermediairesAsync(execControleOfId);
        if (execOf == null || (execOf.Statut != "EN_COURS" && execOf.Statut != "REGLAGE")) return false;

        execOf.EstEnReglage = true;
        execOf.Statut = "REGLAGE";
        if (!execOf.DateFin.HasValue)
        {
            execOf.DateFin = DateTime.Now;
        }
        
        var futureUnansweredOccurrences = execOf.ExecPrelevementIntermediaires
            .Where(o => o.HeureNotifPrevue > DateTime.Now && !o.EstRepondu)
            .ToList();
            
        foreach (var occ in futureUnansweredOccurrences)
        {
            execOf.ExecPrelevementIntermediaires.Remove(occ);
        }

        await _operateurRepository.SaveChangesAsync();

        await _occurrenceService.GenererOccurrencesReglageCoursAsync(execOf.Id);

        return true;
    }

    public async Task<(bool Success, string Message)> ReprendreOfAsync(Guid execControleOfId)
    {
        var execOf = await _operateurRepository.GetExecOfWithIntermediairesAsync(execControleOfId);
        if (execOf == null || (execOf.Statut != "EN_COURS" && execOf.Statut != "REGLAGE")) return (false, "Impossible de reprendre cet OF.");

        if (execOf.EstEnReglage || execOf.Statut == "REGLAGE") 
        {
            var reglageOccurrences = execOf.ExecPrelevementIntermediaires.Where(o => o.TrancheHoraire.StartsWith("REGLAGE")).ToList();
            
            bool hasUnansweredReglage = reglageOccurrences.Any(o => !o.EstRepondu);

            if (hasUnansweredReglage)
            {
                return (false, "Vous devez effectuer et valider tous les contrôles de réglage avant de pouvoir reprendre la production.");
            }

            var latestReglagePerSection = reglageOccurrences
                .GroupBy(o => o.SectionId)
                .Select(g => g.OrderByDescending(o => o.HeureNotifPrevue).First())
                .ToList();

            if (latestReglagePerSection.Any(o => o.Resultat != "C"))
            {
                return (false, "Les caractéristiques au réglage ne sont pas toutes conformes (NC). Veuillez générer un nouveau réglage.");
            }

            execOf.EstEnReglage = false;
            execOf.Statut = "EN_COURS";
            
            execOf.DateFin = DateTime.Now; 
            await _occurrenceService.AjusterDateDebutApresReglageAsync(execOf.Id);
            await _occurrenceService.NettoyerOccurrencesReglageAsync(execOf.Id);
        }
        await _operateurRepository.SaveChangesAsync();
        return (true, "Reprise avec succès.");
    }

    public async Task<bool> MettreEnPauseAsync(Guid execControleOfId, string raison)
    {
        var execOf = await _operateurRepository.GetExecOfWithIntermediairesAsync(execControleOfId);
        if (execOf == null || execOf.Statut != "EN_COURS") return false;

        execOf.Statut = "EN_PAUSE";
        execOf.DateFin = DateTime.Now;

        await _occurrenceService.AjouterRemarqueTrancheEnCoursAsync(execControleOfId, $"Mise en pause : {raison}");

        var magOf = await _operateurRepository.GetMagPreparationOfAsync(execOf.NumeroOf);
        if (magOf != null) magOf.Statut = "EN_PAUSE";

        await _operateurRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReprendreDepuisPauseAsync(Guid execControleOfId)
    {
        var execOf = await _operateurRepository.GetExecOfWithIntermediairesAsync(execControleOfId);
        if (execOf == null || execOf.Statut != "EN_PAUSE") return false;

        var pauseStart = execOf.DateFin ?? DateTime.Now;
        var pauseDuration = DateTime.Now - pauseStart;
        if (pauseDuration < TimeSpan.Zero) pauseDuration = TimeSpan.Zero;

        execOf.Statut = "EN_COURS";
        execOf.TempsPauseTotalMinutes += pauseDuration.TotalMinutes;
        execOf.DateFin = null;

        var magOf = await _operateurRepository.GetMagPreparationOfAsync(execOf.NumeroOf);
        if (magOf != null) magOf.Statut = "EN_COURS";

        await _operateurRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CloturerOfAsync(Guid execControleOfId)
    {
        var execOf = await _operateurRepository.GetExecOfWithIntermediairesAsync(execControleOfId);

        if (execOf == null) return false;

        execOf.Statut = "CLOTURE";
        execOf.DateFin = DateTime.Now;

        await _occurrenceService.AjouterRemarqueTrancheEnCoursAsync(execControleOfId, "Clôture de l'opération pour cet OF");

        var unanswered = execOf.ExecPrelevementIntermediaires.Where(n => !n.EstRepondu).ToList();
        foreach (var occ in unanswered)
        {
            occ.Resultat = "IGNORE";
            occ.EstRepondu = true;
            occ.HeureReponse = DateTime.Now;
        }

        if (unanswered.Any())
        {
            _occurrenceRepository.RemoveIntermediaires(unanswered);
        }

        var statuts = await _operateurRepository.GetDocumentStatutsAsync(execControleOfId);
        foreach (var s in statuts)
        {
            s.EstTermine = true;
            s.DateTermine = DateTime.Now;
        }

        bool allClosed = await _operateurRepository.AreAllOperationsClosedAsync(execOf.NumeroOf);
        
        if (allClosed)
        {
            var magOf = await _operateurRepository.GetMagPreparationOfAsync(execOf.NumeroOf);
            if (magOf != null) 
            {
                magOf.Statut = "TERMINE";
            }
        }

        await _operateurRepository.SaveChangesAsync();
        return true;
    }

    private async Task<ExecControleOfDto> MapToDto(Guid execOfId)
    {
        var execOf = await _operateurRepository.GetExecOfByIdAsync(execOfId);
        if (execOf == null) throw new Exception("OF introuvable");
        
        bool aDesControlesReglage = execOf.PlanSourceId.HasValue 
            ? await _operateurRepository.HasReglageSectionsAsync(execOf.PlanSourceId.Value) 
            : false;

        return new ExecControleOfDto
        {
            Id = execOf.Id,
            NumeroOf = execOf.NumeroOf,
            OperationCode = execOf.OperationCode,
            MachineCode = execOf.MachineCode,
            PosteCode = execOf.PosteCode,
            Statut = execOf.Statut,
            DateDebut = execOf.DateDebut,
            A_Des_Controles_Reglage = aDesControlesReglage,
            EstEnReglage = execOf.EstEnReglage
        };
    }

    public async Task<bool> UpdatePlanLigneAsync(Guid ligneId, UpdatePlanLigneDto dto)
    {
        var ligne = await _operateurRepository.GetPlanLigneAsync(ligneId);
        if (ligne == null) return false;

        if (dto.LibelleAffiche != null) ligne.LibelleAffiche = dto.LibelleAffiche;
        if (dto.LimiteSpecTexte != null) ligne.LimiteSpecTexte = dto.LimiteSpecTexte;

        await _operateurRepository.SaveChangesAsync();
        return true;
    }

    public async Task<object> VerifierPlanActifAsync(string articleCode, string? operationCode = null)
    {
        var planFab = await _operateurRepository.GetPlanActifAsync(articleCode, operationCode);
        if (planFab == null) return new { existe = false, longueur = (double?)null, diametre = (double?)null };

        var lignes = planFab.PlanFabricationSections.SelectMany(s => s.PlanFabricationLignes).ToList();

        double? longueur = null;
        var ligneLongueur = lignes.FirstOrDefault(l => l.LibelleAffiche != null && l.LibelleAffiche.Contains("longueur", StringComparison.OrdinalIgnoreCase));
        if (ligneLongueur?.LibelleAffiche != null)
        {
            var parts = ligneLongueur.LibelleAffiche.Split('=');
            if (parts.Length > 1 && double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double l)) longueur = l;
        }

        double? diametre = null;
        var ligneDiametre = lignes.FirstOrDefault(l => l.LibelleAffiche != null && (l.LibelleAffiche.Contains("Diametre", StringComparison.OrdinalIgnoreCase) || l.LibelleAffiche.Contains("Diamètre", StringComparison.OrdinalIgnoreCase)));
        if (ligneDiametre?.LibelleAffiche != null)
        {
            var parts = ligneDiametre.LibelleAffiche.Split('=');
            if (parts.Length > 1 && double.TryParse(parts[1].Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d)) diametre = d;
        }

        return new { existe = true, longueur, diametre };
    }

    public async Task<bool> IgnorerTrancheAsync(Guid execControleOfId, string trancheHoraire, string matriculeOperateur, string? raison = null)
    {
        var execOf = await _operateurRepository.GetExecOfWithIntermediairesAsync(execControleOfId);
        if (execOf == null) return false;

        var cleanTranche = trancheHoraire.Split('|')[0];
        var occurrences = execOf.ExecPrelevementIntermediaires
            .Where(o => (o.TrancheHoraire == trancheHoraire || o.TrancheHoraire.StartsWith(cleanTranche) || o.TrancheHoraire.Split('|')[0] == cleanTranche) && !o.EstRepondu)
            .ToList();

        var ids = occurrences.Select(o => o.Id).ToList();

        return await _occurrenceService.IgnorerOccurrencesAsync(ids, matriculeOperateur, raison);
    }

    public async Task<bool> DeclarerTrancheEnReglageAsync(Guid execControleOfId, string trancheHoraire, string matriculeOperateur)
    {
        var execOf = await _operateurRepository.GetExecOfWithIntermediairesAsync(execControleOfId);
        if (execOf == null) return false;

        var cleanTranche = trancheHoraire.Split('|')[0];
        var occurrences = execOf.ExecPrelevementIntermediaires
            .Where(o => (o.TrancheHoraire == trancheHoraire || o.TrancheHoraire.StartsWith(cleanTranche) || o.TrancheHoraire.Split('|')[0] == cleanTranche) && !o.EstRepondu)
            .ToList();

        var ids = occurrences.Select(o => o.Id).ToList();

        bool success = await _occurrenceService.DeclarerOccurrencesReglageAsync(ids, matriculeOperateur);
        if (success)
        {
            await MettreEnReglageAsync(execControleOfId);
        }
        return success;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services;

public class OperateurService : IOperateurService
{
    private readonly IOperateurRepository _operateurRepository;
    private readonly IOccurrenceService _occurrenceService;

    public OperateurService(IOperateurRepository operateurRepository, IOccurrenceService occurrenceService)
    {
        _operateurRepository = operateurRepository;
        _occurrenceService = occurrenceService;
    }

    public async Task<ExecControleOfDto> DemarrerOfAsync(DemarrerOfRequest request)
    {
        // Récupérer le plan de fabrication actif
        var of = await _operateurRepository.GetOfAsync(request.NumeroOf);

        if (of == null) throw new Exception("OF introuvable");

        if (!await _operateurRepository.CanStartOperationAsync(request.NumeroOf, request.OperationCode))
        {
            throw new Exception("Vous ne pouvez pas démarrer cette opération car l'opération précédente dans la gamme n'a pas encore commencé.");
        }

        var planFab = await _operateurRepository.GetPlanActifAsync(of.CodeArticle, request.OperationCode);

        if (planFab == null) throw new Exception("Aucun plan de fabrication actif pour cet article");

        // Si Tronçonnage, on enregistre les paramètres s'ils sont fournis
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

        bool aDesControlesReglage = planFab.PlanFabricationSections.Any(s => s.TypeSection?.Code == "REGLAGE" || s.TypeSection?.Code == "REGLAGE_PROD");

        var execOf = new ExecControleOf
        {
            NumeroOf = request.NumeroOf,
            OperationCode = request.OperationCode,
            NumEquipe = request.NumEquipe,
            PlanSourceId = planFab.Id,
            TypePlan = "FAB",
            Statut = aDesControlesReglage ? "REGLAGE" : "EN_COURS",
            EstEnReglage = aDesControlesReglage,
            DateDebut = DateTime.Now
        };

        if (request.OperationCode == "USI" || request.OperationCode == "TRN" || request.OperationCode == "ESTOMP" || request.OperationCode == "TRONC")
        {
            execOf.MachineCodePrevu = string.IsNullOrEmpty(planFab.MachineDefautCode) ? request.MachineCode : planFab.MachineDefautCode;
            execOf.MachineCode = request.MachineCode;
        }
        else if (request.OperationCode == "ASS")
        {
            execOf.PosteCodePrevu = request.PosteCode;
            execOf.PosteCode = request.PosteCode;
        }

        _operateurRepository.AddExecControleOf(execOf);
        await _operateurRepository.SaveChangesAsync(); // Pour avoir l'Id généré

        // Génération des occurrences initiales
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
            execOf.DateFin = DateTime.Now; // Stocker temporairement l'heure de début du réglage si pas déjà stocké
        }
        
        // Supprimer toutes les occurrences prévues dans le futur qui n'ont pas encore été répondues
        // Elles seront régénérées proprement à la reprise en utilisant la nouvelle Baseline
        var futureUnansweredOccurrences = execOf.ExecPrelevementIntermediaires
            .Where(o => o.HeureNotifPrevue > DateTime.Now && !o.EstRepondu)
            .ToList();
            
        foreach (var occ in futureUnansweredOccurrences)
        {
            execOf.ExecPrelevementIntermediaires.Remove(occ);
        }

        await _operateurRepository.SaveChangesAsync();

        // Générer les occurrences de réglage pour cette phase
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
            
            // Vérifier s'il y a des occurrences de réglage non répondues
            bool hasUnansweredReglage = reglageOccurrences.Any(o => !o.EstRepondu);

            if (hasUnansweredReglage)
            {
                return (false, "Vous devez effectuer et valider tous les contrôles de réglage avant de pouvoir reprendre la production.");
            }

            // Vérifier que le dernier contrôle de réglage pour chaque section est conforme (C)
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
            
            // Calculer la durée du réglage pour décaler DateDebut (gèle le temps simulé)
            var reglageStart = execOf.DateFin ?? DateTime.Now;
            var reglageDuration = DateTime.Now - reglageStart;
            execOf.DateDebut = execOf.DateDebut.Add(reglageDuration);

            // On réinitialise la baseline pour que le temps passé en réglage ne soit pas compté comme du temps de production
            execOf.DateFin = DateTime.Now; 
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
        execOf.DateFin = DateTime.Now; // Utiliser DateFin temporairement pour stocker l'heure de début de pause

        // Supprimer toutes les occurrences prévues dans le futur qui n'ont pas encore été répondues
        // Elles seront régénérées proprement à la reprise en utilisant la nouvelle Baseline
        var futureUnansweredOccurrences = execOf.ExecPrelevementIntermediaires
            .Where(o => o.HeureNotifPrevue > DateTime.Now && !o.EstRepondu)
            .ToList();
            
        foreach (var occ in futureUnansweredOccurrences)
        {
            execOf.ExecPrelevementIntermediaires.Remove(occ);
        }

        // Ajouter la raison dans la tranche en cours (ou la plus récente)
        await _occurrenceService.AjouterRemarqueTrancheEnCoursAsync(execControleOfId, $"Mise en pause : {raison}");

        // Synchroniser le statut dans Mag_PreparationOF
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

        execOf.Statut = "EN_COURS";
        execOf.DateDebut = execOf.DateDebut.Add(pauseDuration); // Gèle le temps logique simulé
        execOf.DateFin = null; // La date de fin de pause est effacée, DateDebut est la seule référence
        
        // Les occurrences déjà créées avant la pause restent intactes (on ne modifie pas leur HeureNotifPrevue)

        // Resynchroniser le statut dans Mag_PreparationOF
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

        // Ajouter la remarque dans la tranche en cours
        await _occurrenceService.AjouterRemarqueTrancheEnCoursAsync(execControleOfId, "Clôture de l'opération pour cet OF");

        foreach (var occ in execOf.ExecPrelevementIntermediaires.Where(n => !n.EstRepondu))
        {
            occ.Resultat = "IGNORE";
        }

        // On vérifie si TOUTES les opérations de l'OF sont désormais clôturées
        // Si oui, on ferme l'OF globalement. Sinon, on le laisse ouvert pour les autres opérations.
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

    public async Task<IEnumerable<PosteTravailDto>> GetPostesDisponiblesAsync()
    {
        var postes = await _operateurRepository.GetPostesDisponiblesAsync();
        return postes.Select(p => new PosteTravailDto { CodePoste = p.CodePoste, Libelle = p.Libelle });
    }

    public async Task<IEnumerable<MachineDto>> GetMachinesPourPosteAsync(string posteCode)
    {
        var poste = await _operateurRepository.GetPosteWithMachinesAsync(posteCode);

        if (poste == null) return new List<MachineDto>();

        return poste.CodeMachines
            .Where(m => m.Actif)
            .Select(m => new MachineDto { CodeMachine = m.CodeMachine, Libelle = m.Libelle })
            .ToList();
    }

    private async Task<ExecControleOfDto> MapToDto(Guid execOfId)
    {
        var execOf = await _operateurRepository.GetExecOfByIdAsync(execOfId);
        if (execOf == null) throw new Exception("OF introuvable");
        
        bool aDesControlesReglage = await _operateurRepository.HasReglageSectionsAsync(execOf.PlanSourceId);

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

    public async Task<IEnumerable<OperateurOfDto>> GetAllOfOperationsDisponiblesAsync()
    {
        return await _operateurRepository.GetAllOfOperationsDisponiblesAsync();
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

        var occurrences = execOf.ExecPrelevementIntermediaires
            .Where(o => o.TrancheHoraire == trancheHoraire && !o.EstRepondu)
            .ToList();

        var ids = occurrences.Select(o => o.Id).ToList();

        return await _occurrenceService.IgnorerOccurrencesAsync(ids, matriculeOperateur, raison);
    }

    public async Task<bool> DeclarerTrancheEnReglageAsync(Guid execControleOfId, string trancheHoraire, string matriculeOperateur)
    {
        var execOf = await _operateurRepository.GetExecOfWithIntermediairesAsync(execControleOfId);
        if (execOf == null) return false;

        var occurrences = execOf.ExecPrelevementIntermediaires
            .Where(o => o.TrancheHoraire == trancheHoraire && !o.EstRepondu)
            .ToList();

        var ids = occurrences.Select(o => o.Id).ToList();

        return await _occurrenceService.DeclarerOccurrencesReglageAsync(ids, matriculeOperateur);
    }
}

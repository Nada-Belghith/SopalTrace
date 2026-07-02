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

        var planFab = await _operateurRepository.GetPlanActifAsync(of.CodeArticle);

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

        var execOf = new ExecControleOf
        {
            NumeroOf = request.NumeroOf,
            OperationCode = request.OperationCode,
            NumEquipe = request.NumEquipe,
            PlanSourceId = planFab.Id,
            TypePlan = "FAB",
            Statut = "EN_COURS",
            DateDebut = DateTime.Now
        };

        if (request.OperationCode == "USI" || request.OperationCode == "TRN" || request.OperationCode == "ESTOMP" || request.OperationCode == "TRONC")
        {
            execOf.MachineCodePrevu = planFab.MachineDefautCode;
            execOf.MachineCode = request.MachineCode;
        }
        else if (request.OperationCode == "ASS")
        {
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
        var execOf = await _operateurRepository.GetExecOfByIdAsync(execControleOfId);
        if (execOf == null || execOf.Statut != "EN_COURS") return false;

        execOf.EstEnReglage = true;
        await _operateurRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReprendreOfAsync(Guid execControleOfId)
    {
        var execOf = await _operateurRepository.GetExecOfByIdAsync(execControleOfId);
        if (execOf == null || execOf.Statut != "EN_COURS") return false;

        execOf.EstEnReglage = false;
        await _operateurRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CloturerOfAsync(Guid execControleOfId)
    {
        var execOf = await _operateurRepository.GetExecOfWithIntermediairesAsync(execControleOfId);

        if (execOf == null) return false;

        execOf.Statut = "TERMINE";
        execOf.DateFin = DateTime.Now;

        foreach (var occ in execOf.ExecPrelevementIntermediaires.Where(n => !n.EstRepondu))
        {
            occ.Resultat = "ANNULE";
        }

        var of = await _operateurRepository.GetOfAsync(execOf.NumeroOf);
        if (of != null)
        {
            of.StatutOf = "TERMINE";
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

    public async Task<bool> VerifierPlanActifAsync(string articleCode)
    {
        var planFab = await _operateurRepository.GetPlanActifAsync(articleCode);
        return planFab != null;
    }
}

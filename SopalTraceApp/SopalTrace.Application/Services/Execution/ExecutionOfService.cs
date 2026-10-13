using System;
using SopalTrace.Application.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Services.Execution;

public class ExecutionOfService : IExecutionOfService
{
    private readonly IOperateurRepository _operateurRepository;
    private readonly IOccurrenceService _occurrenceService;
    private readonly IOccurrenceRepository _occurrenceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExecutionOfService(IOperateurRepository operateurRepository, IOccurrenceService occurrenceService, IOccurrenceRepository occurrenceRepository, IUnitOfWork unitOfWork)
    {
        _operateurRepository = operateurRepository;
        _occurrenceService = occurrenceService;
        _occurrenceRepository = occurrenceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ExecControleOfDto> DemarrerOfAsync(DemarrerOfRequest request)
    {
        var of = await _operateurRepository.GetOfAsync(request.NumeroOf);
        if (of == null) throw new RessourceIntrouvableException("OF introuvable.");

        if (!await _operateurRepository.CanStartOperationAsync(request.NumeroOf, request.OperationCode))
        {
            throw new RegleMetierException("Vous ne pouvez pas démarrer cette opération car l'opération précédente dans la gamme n'a pas encore commencé.");
        }

        var operation = await _operateurRepository.GetOperationAsync(request.OperationCode)
                        ?? throw new RessourceIntrouvableException("Opération introuvable.");
        if (operation.AvecMachine && string.IsNullOrWhiteSpace(request.MachineCode))
            throw new RegleMetierException($"La machine est obligatoire pour l'opération {operation.Libelle}.");

        var planFab = await _operateurRepository.GetPlanActifAsync(of.CodeArticle, request.OperationCode);
        if (planFab == null) throw new RessourceIntrouvableException("Aucun plan de fabrication actif pour cet article.");

        bool aDesControlesReglage = planFab.DocumentSections.Any(s => 
            (s.TypeSection?.TypeNormalise == SectionTypes.Reglage || s.TypeSection?.TypeNormalise == SectionTypes.ReglageProd) 
            && s.DocumentLignes.Any());

        var execOf = new ExecControleOf
        {
            NumeroOf = request.NumeroOf,
            OperationCode = request.OperationCode,
            PlanSourceId = planFab.Id,
            TypeOf = TypesOf.Fabrication,
            Statut = aDesControlesReglage ? StatutsExecution.Reglage : StatutsExecution.EnCours,
            DateDebut = DateTime.Now,
            SuspendedAt = aDesControlesReglage ? DateTime.Now : null,
            // Tronçonnage : cotes saisies au démarrage, conservées sur l'exécution (le plan n'est pas modifié).
            LongueurNominale = operation.AvecCotesTronconnage ? (decimal?)request.Longueur : null,
            DiametreNominal = operation.AvecCotesTronconnage ? (decimal?)request.Diametre : null
        };

        if (operation.AvecMachine)
        {
            execOf.MachineCodePrevu = string.IsNullOrEmpty(planFab.MachineCode) ? request.MachineCode : planFab.MachineCode;
            execOf.MachineCode = request.MachineCode;
        }
        else if (request.OperationCode == CodesOperation.Assemblage)
        {
            // Les postes sont portés uniquement par Exec_ControleOf_Poste.
            var inputPostes = request.PosteCode ?? request.MachineCode ?? "";
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

        // Contrôles de production : planifiés par le serveur dès que l'OF est en cours (après le réglage).
        if (aDesControlesReglage)
        {
            await _occurrenceService.GenererOccurrencesReglageCoursAsync(execOf.Id);
        }

        return await MapToDto(execOf.Id);
    }

    public async Task<bool> MettreEnReglageAsync(Guid execControleOfId)
    {
        var execOf = await _operateurRepository.GetExecOfWithIntermediairesAsync(execControleOfId);
        if (execOf == null || (execOf.Statut != StatutsExecution.EnCours && execOf.Statut != StatutsExecution.Reglage)) return false;

        var debutReglage = DateTime.Now;
        execOf.Statut = StatutsExecution.Reglage;
        execOf.SuspendedAt = debutReglage;

        var futureUnansweredOccurrences = execOf.ExecPrelevementIntermediaires
            .Where(o => !o.TrancheHoraire.StartsWith("REGLAGE") && !o.EstRepondu && o.HeureNotifPrevue >= debutReglage)
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
        if (execOf == null || (execOf.Statut != StatutsExecution.EnCours && execOf.Statut != StatutsExecution.Reglage && execOf.Statut != StatutsExecution.EnPause)) return (false, "Impossible de reprendre cet OF.");

        var session = await _operateurRepository.GetActiveSessionAsync(execControleOfId);
        if (session != null && session.Statut == StatutsSession.EnPause)
        {
            if (session.SuspendedAt.HasValue)
            {
                session.TempsPauseTotalMinutes += (DateTime.Now - session.SuspendedAt.Value).TotalMinutes;
                session.SuspendedAt = null;
            }
            session.Statut = StatutsSession.EnCours;
        }

        if (execOf.Statut == StatutsExecution.EnPause)
        {
            if (execOf.SuspendedAt.HasValue)
            {
                execOf.TempsPauseTotalMinutes += (DateTime.Now - execOf.SuspendedAt.Value).TotalMinutes;
            }
            execOf.SuspendedAt = null;
            execOf.Statut = StatutsExecution.EnCours;
        }

        if (execOf.Statut == StatutsExecution.Reglage)
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

            if (latestReglagePerSection.Any(o => o.Resultat != ResultatsControle.Conforme))
            {
                return (false, "Les caractéristiques au réglage ne sont pas toutes conformes (NC). Veuillez générer un nouveau réglage.");
            }

            execOf.Statut = StatutsExecution.EnCours;
            execOf.SuspendedAt = null;
            await _occurrenceService.NettoyerOccurrencesReglageAsync(execOf.Id);
        }
        await _operateurRepository.SaveChangesAsync();
        return (true, "Reprise avec succès.");
    }

    public async Task<bool> MettreEnPauseAsync(Guid execControleOfId, string raison, string typePause)
    {
        if (!TypesPause.Autorises.Contains(typePause))
            throw new RegleMetierException("Type de pause invalide.");

        var execOf = await _operateurRepository.GetExecOfWithIntermediairesAsync(execControleOfId);
        if (execOf == null || execOf.Statut != StatutsExecution.EnCours) return false;

        // Plusieurs enregistrements successifs (remarque de tranche, statuts) : tout ou rien.
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var maintenant = DateTime.Now;
            var session = await _operateurRepository.GetActiveSessionAsync(execControleOfId);
            if (session != null)
            {
                session.Statut = StatutsSession.EnPause;
                session.SuspendedAt = maintenant;
                // Seule la pause déjeuner rend due la vérification machine « après pause » à la reprise.
                if (typePause == TypesPause.Dejeuner) session.HadLunchBreak = true;
            }

            execOf.Statut = StatutsExecution.EnPause;
            execOf.SuspendedAt = maintenant;

            await _occurrenceService.AjouterRemarqueTrancheEnCoursAsync(execControleOfId, $"Mise en pause [{typePause}] : {raison}");

            await _operateurRepository.SaveChangesAsync();
            return true;
        });
    }

    public async Task<bool> ReprendreDepuisPauseAsync(Guid execControleOfId, string? equipe = null)
    {
        var execOf = await _operateurRepository.GetExecOfWithIntermediairesAsync(execControleOfId);
        if (execOf == null) return false;

        // Plusieurs équipes peuvent être en pause : on reprend celle demandée, jamais pendant qu'une autre produit.
        var sessions = await _operateurRepository.GetSessionsOuvertesAsync(execControleOfId);
        var enProduction = sessions.FirstOrDefault(s => s.Statut == StatutsSession.EnCours);
        var aReprendre = string.IsNullOrWhiteSpace(equipe)
            ? sessions.Where(s => s.Statut == StatutsSession.EnPause).OrderByDescending(s => s.SuspendedAt).FirstOrDefault()
            : sessions.FirstOrDefault(s => s.Statut == StatutsSession.EnPause && string.Equals(s.Equipe, equipe, StringComparison.OrdinalIgnoreCase));
        if (enProduction != null && aReprendre != null)
            throw new EtatExecutionInvalideException(
                $"L'équipe {enProduction.Equipe} produit actuellement sur cet OF : reprenez après sa pause ou sa fin de poste.");
        if (execOf.Statut != StatutsExecution.EnPause || (sessions.Count > 0 && aReprendre == null)) return false;

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var maintenant = DateTime.Now;
            var pauseDuration = maintenant - (execOf.SuspendedAt ?? maintenant);
            if (pauseDuration < TimeSpan.Zero) pauseDuration = TimeSpan.Zero;

            execOf.Statut = StatutsExecution.EnCours;
            execOf.TempsPauseTotalMinutes += pauseDuration.TotalMinutes;
            execOf.SuspendedAt = null;

            var session = aReprendre;
            if (session != null && session.Statut == StatutsSession.EnPause)
            {
                var sessionPauseDuration = maintenant - (session.SuspendedAt ?? maintenant);
                if (sessionPauseDuration < TimeSpan.Zero) sessionPauseDuration = TimeSpan.Zero;

                session.Statut = StatutsSession.EnCours;
                session.TempsPauseTotalMinutes += sessionPauseDuration.TotalMinutes;
                session.SuspendedAt = null;
            }

            await _operateurRepository.SaveChangesAsync();
            return true;
        });
    }

    public async Task<bool> TerminerPosteAsync(Guid execControleOfId)
    {
        var execOf = await _operateurRepository.GetExecOfByIdAsync(execControleOfId);
        if (execOf == null || execOf.Statut == StatutsExecution.Cloture) return false;

        var session = await _operateurRepository.GetActiveSessionAsync(execControleOfId);
        if (session == null) return false;

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var maintenant = DateTime.Now;
            session.Statut = StatutsSession.Termine;
            session.DateFin = maintenant;
            session.SuspendedAt = null;

            // D'autres équipes sont en pause sur l'OF : il reste en pause jusqu'à la reprise de l'une d'elles.
            var autres = (await _operateurRepository.GetSessionsOuvertesAsync(execControleOfId)).Where(s => s.Id != session.Id).ToList();
            if (autres.Count > 0 && autres.All(s => s.Statut == StatutsSession.EnPause))
            {
                execOf.Statut = StatutsExecution.EnPause;
                execOf.SuspendedAt = maintenant;
            }

            // Trace la fin de poste de l'équipe sans clôturer l'OF.
            await _occurrenceService.AjouterRemarqueTrancheEnCoursAsync(execControleOfId, "Fin de poste équipe (Session clôturée).");

            await _operateurRepository.SaveChangesAsync();
            return true;
        });
    }

    public async Task<bool> CloturerOfAsync(Guid execControleOfId)
    {
        var execOf = await _operateurRepository.GetExecOfWithIntermediairesAsync(execControleOfId);
        if (execOf == null || execOf.Statut == StatutsExecution.Cloture) return false;

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var maintenant = DateTime.Now;
            // Toutes les équipes encore ouvertes sont clôturées avec l'OF (une clôture forcée par un
            // administrateur peut laisser une équipe en pause ; l'assistant l'exige avant).
            foreach (var session in await _operateurRepository.GetSessionsOuvertesAsync(execControleOfId))
            {
                session.Statut = StatutsSession.Termine;
                session.DateFin = maintenant;
                session.SuspendedAt = null;
            }

            execOf.Statut = StatutsExecution.Cloture;
            execOf.DateFin = maintenant;
            execOf.SuspendedAt = null;

            await _occurrenceService.AjouterRemarqueTrancheEnCoursAsync(execControleOfId, "Clôture de l'opération pour cet OF");

            // Les occurrences non répondues n'ont plus d'objet une fois l'OF clôturé.
            var nonRepondues = execOf.ExecPrelevementIntermediaires.Where(n => !n.EstRepondu).ToList();
            if (nonRepondues.Count > 0)
            {
                _occurrenceRepository.RemoveIntermediaires(nonRepondues);
            }

            foreach (var s in await _operateurRepository.GetDocumentStatutsAsync(execControleOfId))
            {
                s.EstTermine = true;
                s.DateTermine = maintenant;
            }

            await _operateurRepository.SaveChangesAsync();

            return true;
        });
    }

    private async Task<ExecControleOfDto> MapToDto(Guid execOfId)
    {
        var execOf = await _operateurRepository.GetExecOfByIdAsync(execOfId);
        if (execOf == null) throw new RessourceIntrouvableException("OF introuvable.");
        
        bool aDesControlesReglage = execOf.PlanSourceId.HasValue 
            ? await _operateurRepository.HasReglageSectionsAsync(execOf.PlanSourceId.Value) 
            : false;

        return new ExecControleOfDto
        {
            Id = execOf.Id,
            NumeroOf = execOf.NumeroOf,
            OperationCode = execOf.OperationCode,
            MachineCode = execOf.MachineCode,
            PosteCode = string.Join(",", await _operateurRepository.GetPostesForExecutionAsync(execOf.Id)),
            Statut = execOf.Statut,
            DateDebut = execOf.DateDebut,
            A_Des_Controles_Reglage = aDesControlesReglage,
            EstEnReglage = execOf.Statut == StatutsExecution.Reglage
        };
    }

    public async Task<object> VerifierPlanActifAsync(string articleCode, string? operationCode = null)
    {
        var planFab = await _operateurRepository.GetPlanActifAsync(articleCode, operationCode);
        if (planFab == null) return new { existe = false, longueur = (double?)null, diametre = (double?)null };

        // Cote du plan (écrite par la qualité) en priorité, sinon celle du dernier OF du même article et de la même opération.
        var lignes = planFab.DocumentSections.SelectMany(s => s.DocumentLignes).ToList();
        var precedentes = await _operateurRepository.GetDernieresCotesTronconnageAsync(articleCode, operationCode);
        double? longueur = (double?)(CotesTronconnage.Lire(lignes.FirstOrDefault(l => CotesTronconnage.EstLongueur(l.LibelleAffiche))?.LibelleAffiche)
                                     ?? precedentes.Longueur);
        double? diametre = (double?)(CotesTronconnage.Lire(lignes.FirstOrDefault(l => CotesTronconnage.EstDiametre(l.LibelleAffiche))?.LibelleAffiche)
                                     ?? precedentes.Diametre);

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

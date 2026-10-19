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
    private readonly IDemarragePosteRepository _postes;

    public ExecutionOfService(IOperateurRepository operateurRepository, IOccurrenceService occurrenceService, IOccurrenceRepository occurrenceRepository,
        IUnitOfWork unitOfWork, IDemarragePosteRepository postes)
    {
        _operateurRepository = operateurRepository;
        _occurrenceService = occurrenceService;
        _occurrenceRepository = occurrenceRepository;
        _unitOfWork = unitOfWork;
        _postes = postes;
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

        // Assemblage : la reprise est celle d'une équipe (ReprendreDepuisPauseAsync).
        if (execOf.TypeOf == TypesOf.Assemblage) return (false, "Reprenez la production depuis l'assistant de votre équipe.");

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

    public async Task<bool> MettreEnPauseAsync(Guid execControleOfId, string raison, string typePause, string? equipe = null)
    {
        if (!TypesPause.Autorises.Contains(typePause))
            throw new RegleMetierException("Type de pause invalide.");

        var execOf = await _operateurRepository.GetExecOfWithIntermediairesAsync(execControleOfId);
        if (execOf == null || execOf.Statut != StatutsExecution.EnCours) return false;

        var ouvertes = execOf.TypeOf == TypesOf.Assemblage ? await _postes.GetSessionsOuvertesAsync(execControleOfId) : new List<ExecControleOfSession>();
        var session = execOf.TypeOf == TypesOf.Assemblage ? SessionEquipe(ouvertes, equipe) : null;
        if (execOf.TypeOf == TypesOf.Assemblage && session?.Statut != StatutsSession.EnCours) return false;

        // Plusieurs enregistrements successifs (remarque de tranche, statuts) : tout ou rien.
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var maintenant = DateTime.Now;
            if (session != null)
            {
                session.Statut = StatutsSession.EnPause;
                session.SuspendedAt = maintenant;
                // Seule la pause déjeuner rend due la vérification machine « après pause » à la reprise.
                if (typePause == TypesPause.Dejeuner) session.HadLunchBreak = true;
                AppliquerStatutAssemblage(execOf, ouvertes, maintenant);
            }
            else
            {
                execOf.Statut = StatutsExecution.EnPause;
                execOf.SuspendedAt = maintenant;
            }

            await _occurrenceService.AjouterRemarqueTrancheEnCoursAsync(execControleOfId,
                $"Mise en pause [{typePause}]{(session != null ? $" équipe {session.Equipe}" : "")} : {raison}");

            await _operateurRepository.SaveChangesAsync();
            return true;
        });
    }

    public async Task<bool> ReprendreDepuisPauseAsync(Guid execControleOfId, string? equipe = null)
    {
        var execOf = await _operateurRepository.GetExecOfWithIntermediairesAsync(execControleOfId);
        if (execOf == null) return false;

        if (execOf.TypeOf == TypesOf.Assemblage)
        {
            // Chaque équipe reprend sa propre session (les autres équipes de l'OF n'en dépendent pas).
            var ouvertes = await _postes.GetSessionsOuvertesAsync(execControleOfId);
            var session = SessionEquipe(ouvertes, equipe);
            if (session == null || session.Statut != StatutsSession.EnPause) return false;

            var maintenant = DateTime.Now;
            session.Statut = StatutsSession.EnCours;
            session.TempsPauseTotalMinutes += Math.Max(0, (maintenant - (session.SuspendedAt ?? maintenant)).TotalMinutes);
            session.SuspendedAt = null;
            AppliquerStatutAssemblage(execOf, ouvertes, maintenant);
            await _operateurRepository.SaveChangesAsync();
            return true;
        }

        if (execOf.Statut != StatutsExecution.EnPause) return false;

        var fin = DateTime.Now;
        execOf.Statut = StatutsExecution.EnCours;
        execOf.TempsPauseTotalMinutes += Math.Max(0, (fin - (execOf.SuspendedAt ?? fin)).TotalMinutes);
        execOf.SuspendedAt = null;
        await _operateurRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TerminerPostesAsync(Guid execControleOfId, Guid sessionId, IReadOnlyCollection<string> posteCodes, string? matricule)
    {
        var execOf = await _operateurRepository.GetExecOfByIdAsync(execControleOfId);
        if (execOf == null || execOf.Statut == StatutsExecution.Cloture) return false;

        var ouvertes = await _postes.GetSessionsOuvertesAsync(execControleOfId);
        var session = ouvertes.FirstOrDefault(s => s.Id == sessionId);
        if (session == null) return false;
        var enCours = await _postes.GetPostesEnCoursAsync(sessionId);
        var aTerminer = enCours.Where(p => posteCodes.Contains(p.PosteCode)).ToList();
        if (aTerminer.Count == 0) return false;

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var maintenant = DateTime.Now;
            foreach (var poste in aTerminer)
            {
                poste.EndedAt = maintenant < poste.StartedAt ? poste.StartedAt : maintenant;
                poste.EndedBy = matricule;
            }
            // Postes d'abord : une session terminée ne peut plus en tenir (TR_ExecControleOfSession_Consistency).
            await _operateurRepository.SaveChangesAsync();

            // Dernier poste de l'équipe : sa session se termine ; l'OF reste en cours pour les autres équipes.
            if (aTerminer.Count == enCours.Count)
            {
                session.Statut = StatutsSession.Termine;
                session.DateFin = maintenant;
                session.SuspendedAt = null;
                session.ClosingRequest = null;
            }
            AppliquerStatutAssemblage(execOf, ouvertes, maintenant);

            await _occurrenceService.AjouterRemarqueTrancheEnCoursAsync(execControleOfId,
                $"Fin de poste {string.Join(", ", aTerminer.Select(p => p.PosteCode))} (équipe {session.Equipe}).");

            await _operateurRepository.SaveChangesAsync();
            return true;
        });
    }

    /// <summary>Session ouverte de l'équipe donnée, sinon la seule équipe ouverte sur l'OF.</summary>
    private static ExecControleOfSession? SessionEquipe(List<ExecControleOfSession> ouvertes, string? equipe)
    {
        if (!string.IsNullOrWhiteSpace(equipe))
            return ouvertes.FirstOrDefault(s => string.Equals(s.Equipe, equipe.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? throw new EtatExecutionInvalideException($"L'équipe {equipe.Trim()} n'a pas de session ouverte sur cet OF.");
        return ouvertes.Count <= 1 ? ouvertes.FirstOrDefault()
            : throw new RegleMetierException("Plusieurs équipes travaillent sur cet OF : précisez la vôtre.");
    }

    /// <summary>
    /// OF d'assemblage : en pause seulement quand toutes ses équipes ouvertes le sont (les autres continuent de
    /// produire) ; sans équipe, il reste en cours et attend la suivante.
    /// </summary>
    private static void AppliquerStatutAssemblage(ExecControleOf execOf, IEnumerable<ExecControleOfSession> sessions, DateTime maintenant)
    {
        var ouvertes = sessions.Where(s => s.Statut is StatutsSession.EnCours or StatutsSession.EnPause).ToList();
        var enPause = ouvertes.Count > 0 && ouvertes.All(s => s.Statut == StatutsSession.EnPause);
        if (enPause && execOf.Statut != StatutsExecution.EnPause)
        {
            execOf.Statut = StatutsExecution.EnPause;
            execOf.SuspendedAt = maintenant;
        }
        else if (!enPause && execOf.Statut == StatutsExecution.EnPause)
        {
            execOf.TempsPauseTotalMinutes += Math.Max(0, (maintenant - (execOf.SuspendedAt ?? maintenant)).TotalMinutes);
            execOf.Statut = StatutsExecution.EnCours;
            execOf.SuspendedAt = null;
        }
    }

    public async Task<bool> CloturerOfAsync(Guid execControleOfId)
    {
        var execOf = await _operateurRepository.GetExecOfWithIntermediairesAsync(execControleOfId);
        if (execOf == null || execOf.Statut == StatutsExecution.Cloture) return false;

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var maintenant = DateTime.Now;
            // Toutes les équipes encore ouvertes sont clôturées avec l'OF (une clôture forcée par un
            // administrateur peut laisser une équipe en pause ; l'assistant l'exige avant) : leurs postes d'abord
            // (une session terminée ne peut plus en tenir), puis leurs sessions.
            var ouvertes = await _postes.GetSessionsOuvertesAsync(execControleOfId);
            foreach (var session in ouvertes)
                foreach (var poste in await _postes.GetPostesEnCoursAsync(session.Id))
                    poste.EndedAt = maintenant < poste.StartedAt ? poste.StartedAt : maintenant;
            await _operateurRepository.SaveChangesAsync();
            foreach (var session in ouvertes)
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

            // Documents de l'équipe fermés avec l'OF. Une vérification machine ou un plan d'assemblage garde son état
            // réel : « terminé » y signifie « contrôle de fin fait », jamais coché sans saisie (même clôture forcée).
            foreach (var s in (await _operateurRepository.GetStatutsOfAsync(execControleOfId))
                         .Where(s => s.TypeDocument is not (TypesDocumentExecution.VerifMachine or TypesDocumentExecution.PlanAssemblage)))
            {
                s.EstTermine = true;
                s.DateTermine ??= maintenant;
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

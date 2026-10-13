using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Services.Execution;

/// <summary>Cycle de vie d'un OF d'assemblage (produit fini) côté opérateur.</summary>
public class AssemblageExecutionService : IAssemblageExecutionService
{
    private readonly IOperateurRepository _operateurRepository;
    private readonly IOccurrenceService _occurrenceService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDemarragePosteRepository _demarrages;

    public AssemblageExecutionService(IOperateurRepository operateurRepository, IOccurrenceService occurrenceService, IUnitOfWork unitOfWork,
        IDemarragePosteRepository demarrages)
    {
        _operateurRepository = operateurRepository;
        _occurrenceService = occurrenceService;
        _unitOfWork = unitOfWork;
        _demarrages = demarrages;
    }

    public async Task<IEnumerable<OfAssemblageStatutDto>> GetOfsAssemblageStatutAsync()
    {
        return await _operateurRepository.GetOfsAssemblageStatutAsync();
    }

    public async Task<ExecControleOfDto> DemarrerOfAssemblageAsync(DemarrerOfAssemblageRequest request, string matricule)
    {
        var of = await _operateurRepository.GetOfAsync(request.NumeroOf)
            ?? throw new RessourceIntrouvableException("OF introuvable.");

        if (!await _operateurRepository.OfAssemblageDisponibleAsync(of.NumeroOf))
            throw new RegleMetierException("Cet OF n'est pas disponible pour l'assemblage (non préparé par le magasin ou sans opération d'assemblage).");

        if (await _operateurRepository.ExisteExecutionAssemblageActiveAsync(of.NumeroOf))
            throw new EtatExecutionInvalideException("L'assemblage de cet OF est déjà démarré. Rafraîchissez la liste puis rejoignez la session existante.");

        var postes = await ValiderPostesAsync(request.PosteCodes);
        var maintenant = DateTime.Now;

        var execOf = new ExecControleOf
        {
            NumeroOf = of.NumeroOf,
            OperationCode = CodesOperation.Assemblage,
            TypeOf = TypesOf.Assemblage,
            Statut = StatutsExecution.EnCours,
            DateDebut = maintenant
        };
        foreach (var posteCode in postes)
        {
            execOf.ExecControleOfPostes.Add(new ExecControleOfPoste { PosteCode = posteCode });
        }
        // La première équipe démarre les postes choisis (chacun avec son contrôle « début d'OF »).
        var session = new ExecControleOfSession
        {
            Id = Guid.NewGuid(),
            Equipe = request.Equipe,
            Statut = StatutsSession.EnCours,
            DateDebut = maintenant
        };
        foreach (var posteCode in postes)
            session.ExecSessionPosts.Add(new ExecSessionPost
            {
                Id = Guid.NewGuid(), ExecControleOf = execOf, PosteCode = posteCode, StartedAt = maintenant, StartedBy = matricule
            });
        execOf.ExecControleOfSessions.Add(session);

        // OF, postes et session sont enregistrés ensemble ou pas du tout (les contrôles de production sont
        // planifiés par le serveur quand le poste arrive en production).
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            _operateurRepository.AddExecControleOf(execOf);
            await _operateurRepository.SaveChangesAsync();
            return execOf.Id;
        });

        return new ExecControleOfDto
        {
            Id = execOf.Id,
            NumeroOf = execOf.NumeroOf,
            OperationCode = execOf.OperationCode,
            Statut = execOf.Statut,
            DateDebut = execOf.DateDebut
        };
    }

    /// <summary>
    /// Prise de postes par une équipe. Plusieurs équipes peuvent travailler en même temps sur l'OF, une seule à la
    /// fois par poste : un poste tenu par une autre équipe se libère quand elle le termine. L'équipe peut aussi venir
    /// seulement clôturer l'OF (aucun poste démarré, seuls les contrôles de fin d'OF) si elle est seule sur l'OF.
    /// Une équipe déjà ouverte en production ajoute des postes à sa session.
    /// </summary>
    public async Task AjouterPostesAsync(Guid execControleOfId, AjouterPostesRequest request, string matricule)
    {
        var execOf = await GetExecutionOuverteAsync(execControleOfId);
        if (!request.PourCloturer && request.PosteCodes.Count == 0)
            throw new RegleMetierException("Choisissez au moins un poste sur lequel votre équipe travaille.");

        var ouvertes = await _demarrages.GetSessionsOuvertesAsync(execOf.Id);
        var session = ouvertes.FirstOrDefault(s => string.Equals(s.Equipe, request.Equipe, StringComparison.OrdinalIgnoreCase));
        var cloture = ouvertes.FirstOrDefault(s => s.ClosingRequest == DemandesCloture.FinOf);
        if (cloture != null && cloture != session)
            throw new EtatExecutionInvalideException($"L'équipe {cloture.Equipe} clôture l'OF : aucune autre équipe ne peut commencer.");
        if (session?.Statut == StatutsSession.EnPause)
            throw new EtatExecutionInvalideException("Votre équipe est en pause sur cet OF : reprenez la production depuis l'assistant.");

        var demarrages = await _demarrages.GetDemarragesAsync(execOf.Id);
        if (request.PourCloturer)
        {
            var autres = ouvertes.Where(s => s != session).ToList();
            if (autres.Count > 0)
                throw new EtatExecutionInvalideException("Clôture impossible : " + string.Join(" ; ", autres.Select(s =>
                {
                    var postes = demarrages.Where(d => d.SessionId == s.Id && d.EnCours).Select(d => d.PosteCode).ToList();
                    return $"l'équipe {s.Equipe} {(postes.Count > 0 ? $"tient encore {string.Join(", ", postes)}" : "a une session ouverte")}";
                })) + ". Chaque équipe doit terminer ses postes avant la clôture de l'OF.");
        }

        var demandes = request.PourCloturer ? new List<string>() : await ValiderPostesAsync(request.PosteCodes);
        foreach (var poste in demandes)
        {
            var titulaire = demarrages.FirstOrDefault(d => d.PosteCode == poste && d.EnCours);
            if (titulaire != null && titulaire.SessionId != session?.Id)
                throw new EtatExecutionInvalideException(AvancementAssemblageService.MessagePosteOccupe(poste, titulaire.Equipe));
            if (titulaire == null && session != null && demarrages.Any(d => d.SessionId == session.Id && d.PosteCode == poste))
                throw new EtatExecutionInvalideException(
                    $"Votre équipe a déjà terminé le poste {poste} dans cette prise de poste : terminez vos autres postes, puis ouvrez une nouvelle prise de poste pour le reprendre.");
        }

        var maintenant = DateTime.Now;
        if (session == null)
        {
            session = new ExecControleOfSession
            {
                Id = Guid.NewGuid(),
                ExecControleOfId = execOf.Id,
                Equipe = request.Equipe,
                Statut = StatutsSession.EnCours,
                DateDebut = maintenant,
                ClosingRequest = request.PourCloturer ? DemandesCloture.FinOf : null
            };
            _operateurRepository.AddSession(session);
            // Une équipe en production : l'OF n'est plus en pause (les autres équipes gardent leur pause).
            if (execOf.Statut == StatutsExecution.EnPause)
            {
                execOf.TempsPauseTotalMinutes += Math.Max(0, (maintenant - (execOf.SuspendedAt ?? maintenant)).TotalMinutes);
                execOf.SuspendedAt = null;
                execOf.Statut = StatutsExecution.EnCours;
            }
        }
        else if (request.PourCloturer)
        {
            session.ClosingRequest = DemandesCloture.FinOf;
        }

        var postesExistants = (await _operateurRepository.GetPostesForExecutionAsync(execOf.Id)).ToHashSet();
        foreach (var poste in demandes.Where(p => !postesExistants.Contains(p)))
            _operateurRepository.AddExecControleOfPoste(new ExecControleOfPoste { ExecControleOfId = execOf.Id, PosteCode = poste });

        var tenus = demarrages.Where(d => d.SessionId == session.Id && d.EnCours).Select(d => d.PosteCode).ToHashSet();
        foreach (var poste in demandes.Where(p => !tenus.Contains(p)))
            _demarrages.Ajouter(new ExecSessionPost
            {
                Id = Guid.NewGuid(), SessionId = session.Id, ExecControleOfId = execOf.Id, PosteCode = poste, StartedAt = maintenant, StartedBy = matricule
            });

        await _operateurRepository.SaveChangesAsync();
    }

    public async Task<Guid> InitVerifMachineAsync(Guid execControleOfId, InitVerifMachineRequest request, string matricule)
    {
        var execOf = await GetExecutionOuverteAsync(execControleOfId);

        var postes = await _operateurRepository.GetPostesForExecutionAsync(execOf.Id);
        if (!postes.Contains(request.PosteCode))
            throw new RegleMetierException($"Le poste {request.PosteCode} n'est pas affecté à cet OF.");

        var planId = await _operateurRepository.GetPlanVerifMachineActifIdAsync(request.MachineCode)
            ?? throw new RessourceIntrouvableException($"Aucun plan de vérification actif n'a été trouvé pour la machine {request.MachineCode}. Veuillez le signaler au superviseur.");

        var session = await _demarrages.GetSessionAsync(execOf.Id, request.PosteCode)
            ?? throw new EtatExecutionInvalideException($"Aucune équipe ne tient le poste {request.PosteCode}.");
        if (!await _demarrages.EstEnCoursAsync(session.Id, request.PosteCode))
            throw new EtatExecutionInvalideException(ExecPlanAssemblageService.MessagePosteNonDemarre(request.PosteCode, session.Equipe));
        var aujourdhui = DateTime.Today;

        // Un document par machine, poste et session d'équipe (la session est celle du jour de l'équipe).
        var existant = (await _operateurRepository.GetDocumentStatutsAsync(execOf.Id, session.Id)).FirstOrDefault(s =>
            s.TypeDocument == TypesDocumentExecution.VerifMachine &&
            s.PosteCode == request.PosteCode &&
            s.MachineCode == request.MachineCode);
        if (existant != null) return existant.Id;

        var statut = new ExecControleDocumentStatut
        {
            Id = Guid.NewGuid(),
            ExecControleOfId = execOf.Id,
            PosteCode = request.PosteCode,
            MachineCode = request.MachineCode,
            TypeDocument = TypesDocumentExecution.VerifMachine,
            DocId = planId,
            SessionId = session.Id,
            Equipe = session.Equipe,
            DateExecution = aujourdhui,
            MatriculeOperateur = matricule
        };
        _operateurRepository.AddExecControleDocumentStatut(statut);
        await _operateurRepository.SaveChangesAsync();
        return statut.Id;
    }

    public async Task<IEnumerable<DocumentStatutDto>> GetDocumentsAssemblageStatusAsync(Guid execControleOfId, string? equipe = null)
    {
        // Documents de l'équipe demandée (par défaut la seule équipe ouverte sur l'OF).
        var ouvertes = await _demarrages.GetSessionsOuvertesAsync(execControleOfId);
        var session = !string.IsNullOrWhiteSpace(equipe)
            ? ouvertes.FirstOrDefault(s => string.Equals(s.Equipe, equipe.Trim(), StringComparison.OrdinalIgnoreCase))
            : ouvertes.Count == 1 ? ouvertes[0] : null;
        if (session == null) return Enumerable.Empty<DocumentStatutDto>();
        var statuts = (await _operateurRepository.GetDocumentStatutsAsync(execControleOfId, session.Id)).ToList();

        // Un document resté ouvert un jour précédent est clôturé automatiquement à minuit.
        var aujourdhui = DateTime.Today;
        var documentsExpires = statuts.Where(s => !s.EstTermine && s.DateExecution.HasValue && s.DateExecution.Value.Date < aujourdhui).ToList();
        foreach (var s in documentsExpires)
        {
            s.EstTermine = true;
            s.DateTermine = s.DateExecution!.Value.Date.AddDays(1).AddTicks(-1);
        }
        if (documentsExpires.Count > 0)
        {
            await _operateurRepository.SaveChangesAsync();
        }

        var designations = await _operateurRepository.GetFormulaireDesignationsAsync(
            statuts.Where(s => s.DocId.HasValue && s.TypeDocument != TypesDocumentExecution.Echantillonnage).Select(s => s.DocId!.Value));
        var machineNames = await _operateurRepository.GetMachineLabelsAsync(
            statuts.Where(s => !string.IsNullOrEmpty(s.MachineCode)).Select(s => s.MachineCode!));

        return statuts.Select(s => new DocumentStatutDto
        {
            Id = s.Id,
            TypeDocument = s.TypeDocument,
            PosteCode = s.PosteCode,
            MachineCode = s.MachineCode,
            MachineLibelle = s.MachineCode != null && machineNames.TryGetValue(s.MachineCode, out var libelle) ? libelle : null,
            DocId = s.DocId,
            LibelleFormulaire = s.TypeDocument == TypesDocumentExecution.Echantillonnage
                ? "Document Échantillonnage"
                : s.DocId.HasValue && designations.TryGetValue(s.DocId.Value, out var designation) ? designation : null,
            EstDemarrageTermine = s.EstDemarrageTermine,
            EstPauseTermine = s.EstPauseTermine,
            EstTermine = s.EstTermine,
            DateTermine = s.DateTermine,
            Equipe = s.Equipe,
            DateExecution = s.DateExecution
        });
    }

    public async Task MarquerDocumentTermineAsync(Guid statutId, string matricule, string? periodicite)
    {
        var statut = await GetStatutAsync(statutId);

        var moments = string.IsNullOrEmpty(statut.MachineCode)
            ? new List<string>()
            : await _operateurRepository.GetPeriodicitesPlanMachineAsync(statut.MachineCode);
        bool aApresPause = PeriodiciteMachineHelper.AApresPause(moments);
        bool aFinPoste = PeriodiciteMachineHelper.AFinPoste(moments);

        switch (PeriodicitesControle.Normaliser(periodicite))
        {
            case PeriodicitesControle.ApresPause:
                statut.EstPauseTermine = true;
                if (!aFinPoste) statut.EstTermine = true;
                break;
            case PeriodicitesControle.FinPoste:
                statut.EstTermine = true;
                break;
            default:
                statut.EstDemarrageTermine = true;
                if (!aApresPause && !aFinPoste) statut.EstTermine = true;
                break;
        }

        statut.DateTermine = DateTime.Now;
        statut.MatriculeOperateur = matricule;
        await _operateurRepository.SaveChangesAsync();
    }

    public async Task RouvrirDocumentAsync(Guid statutId, string matricule, string? periodicite)
    {
        var statut = await GetStatutAsync(statutId);

        switch (PeriodicitesControle.Normaliser(periodicite))
        {
            case PeriodicitesControle.ApresPause:
                statut.EstPauseTermine = false;
                break;
            case PeriodicitesControle.FinPoste:
                statut.EstTermine = false;
                break;
            default:
                statut.EstDemarrageTermine = false;
                statut.EstTermine = false;
                break;
        }
        statut.DateTermine = null;
        statut.MatriculeOperateur = matricule;

        await _operateurRepository.SaveChangesAsync();
    }

    private async Task<ExecControleOf> GetExecutionOuverteAsync(Guid execControleOfId)
    {
        var execOf = await _operateurRepository.GetExecOfByIdAsync(execControleOfId)
            ?? throw new RessourceIntrouvableException("Exécution introuvable.");

        if (execOf.Statut == StatutsExecution.Cloture)
            throw new EtatExecutionInvalideException("Cet OF est clôturé : aucune modification n'est possible.");

        return execOf;
    }

    private async Task<ExecControleDocumentStatut> GetStatutAsync(Guid statutId)
    {
        var statut = await _operateurRepository.GetDocumentStatutByIdAsync(statutId)
            ?? throw new RessourceIntrouvableException("Statut document introuvable.");

        if (statut.ExecControleOf.Statut == StatutsExecution.Cloture)
            throw new EtatExecutionInvalideException("Cet OF est clôturé : aucune modification n'est possible.");

        // Seuls les documents de l'équipe qui tient le poste sont modifiables ; les autres sont archivés.
        if (statut.SessionId is not Guid sessionId || statut.PosteCode == null || !await _demarrages.EstEnCoursAsync(sessionId, statut.PosteCode))
            throw new EtatExecutionInvalideException("Ce document appartient à une équipe qui ne tient plus ce poste : il ne peut plus être modifié.");

        return statut;
    }

    private async Task<List<string>> ValiderPostesAsync(IEnumerable<string> posteCodes)
    {
        var demandes = posteCodes.Select(p => p.Trim()).Where(p => p.Length > 0).Distinct().ToList();
        if (demandes.Count == 0) return demandes;

        var actifs = await _operateurRepository.GetCodesPostesActifsAsync(demandes);
        var inconnus = demandes.Where(p => !actifs.Contains(p)).ToList();
        if (inconnus.Count > 0)
            throw new RegleMetierException($"Poste(s) inconnu(s) ou inactif(s) : {string.Join(", ", inconnus)}.");

        return demandes;
    }
}

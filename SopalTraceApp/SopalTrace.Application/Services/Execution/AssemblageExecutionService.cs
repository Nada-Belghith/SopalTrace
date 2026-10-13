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

    public AssemblageExecutionService(IOperateurRepository operateurRepository, IOccurrenceService occurrenceService, IUnitOfWork unitOfWork)
    {
        _operateurRepository = operateurRepository;
        _occurrenceService = occurrenceService;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<OfAssemblageStatutDto>> GetOfsAssemblageStatutAsync()
    {
        return await _operateurRepository.GetOfsAssemblageStatutAsync();
    }

    public async Task<ExecControleOfDto> DemarrerOfAssemblageAsync(DemarrerOfAssemblageRequest request)
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
        execOf.ExecControleOfSessions.Add(new ExecControleOfSession
        {
            Equipe = request.Equipe,
            Statut = StatutsSession.EnCours,
            DateDebut = maintenant
        });

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

    public async Task AjouterPostesAsync(Guid execControleOfId, AjouterPostesRequest request)
    {
        var execOf = await GetExecutionOuverteAsync(execControleOfId);

        // Une seule équipe en production à la fois : une autre équipe peut rejoindre l'OF pendant la pause
        // de la précédente, qui retrouvera son étape à son retour.
        var sessions = await _operateurRepository.GetSessionsOuvertesAsync(execOf.Id);
        var enProduction = sessions.FirstOrDefault(s => s.Statut == StatutsSession.EnCours);
        if (enProduction != null && !string.Equals(enProduction.Equipe, request.Equipe, StringComparison.OrdinalIgnoreCase))
            throw new EtatExecutionInvalideException(
                $"L'équipe {enProduction.Equipe} produit actuellement sur cet OF : elle doit se mettre en pause ou terminer son poste avant qu'une autre équipe ne prenne la main.");

        if (!sessions.Any(s => string.Equals(s.Equipe, request.Equipe, StringComparison.OrdinalIgnoreCase)))
        {
            var maintenant = DateTime.Now;
            _operateurRepository.AddSession(new ExecControleOfSession
            {
                ExecControleOfId = execOf.Id,
                Equipe = request.Equipe,
                Statut = StatutsSession.EnCours,
                DateDebut = maintenant
            });
            // L'OF repart en production avec la nouvelle équipe (la pause de l'équipe précédente est décomptée).
            if (execOf.Statut == StatutsExecution.EnPause)
            {
                if (execOf.SuspendedAt.HasValue) execOf.TempsPauseTotalMinutes += Math.Max(0, (maintenant - execOf.SuspendedAt.Value).TotalMinutes);
                execOf.SuspendedAt = null;
                execOf.Statut = StatutsExecution.EnCours;
            }
        }
        // Équipe déjà présente (en pause) : elle reprendra depuis l'assistant (« Reprendre »).

        var postesExistants = (await _operateurRepository.GetPostesForExecutionAsync(execOf.Id)).ToHashSet();
        var nouveauxPostes = await ValiderPostesAsync(request.PosteCodes.Where(p => !postesExistants.Contains(p)));

        foreach (var poste in nouveauxPostes)
        {
            _operateurRepository.AddExecControleOfPoste(new ExecControleOfPoste
            {
                ExecControleOfId = execOf.Id,
                PosteCode = poste
            });
        }

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

        var session = await _operateurRepository.GetActiveSessionAsync(execOf.Id);
        var aujourdhui = DateTime.Today;

        var existant = (await _operateurRepository.GetDocumentStatutsAsync(execOf.Id)).FirstOrDefault(s =>
            s.TypeDocument == TypesDocumentExecution.VerifMachine &&
            s.PosteCode == request.PosteCode &&
            s.MachineCode == request.MachineCode &&
            s.DateExecution == aujourdhui);
        if (existant != null) return existant.Id;

        var statut = new ExecControleDocumentStatut
        {
            Id = Guid.NewGuid(),
            ExecControleOfId = execOf.Id,
            PosteCode = request.PosteCode,
            MachineCode = request.MachineCode,
            TypeDocument = TypesDocumentExecution.VerifMachine,
            DocId = planId,
            SessionId = session?.Id,
            Equipe = session?.Equipe,
            DateExecution = aujourdhui,
            MatriculeOperateur = matricule
        };
        _operateurRepository.AddExecControleDocumentStatut(statut);
        await _operateurRepository.SaveChangesAsync();
        return statut.Id;
    }

    public async Task<IEnumerable<DocumentStatutDto>> GetDocumentsAssemblageStatusAsync(Guid execControleOfId, string? equipe = null)
    {
        // Documents de l'équipe demandée (elle peut être en pause pendant qu'une autre produit), sinon de la session active.
        Guid? sessionId = null;
        if (!string.IsNullOrWhiteSpace(equipe))
        {
            sessionId = (await _operateurRepository.GetSessionsOuvertesAsync(execControleOfId))
                .FirstOrDefault(s => string.Equals(s.Equipe, equipe, StringComparison.OrdinalIgnoreCase))?.Id;
        }
        var statuts = (await _operateurRepository.GetDocumentStatutsAsync(execControleOfId, sessionId)).ToList();

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

        // Seuls les documents de l'équipe active sont modifiables ; ceux d'une session terminée sont archivés.
        var sessionActive = await _operateurRepository.GetActiveSessionAsync(statut.ExecControleOfId);
        if (statut.SessionId != sessionActive?.Id)
            throw new EtatExecutionInvalideException("Ce document appartient à une session d'équipe terminée : il ne peut plus être modifié.");

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

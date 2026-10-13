using System;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.Dtos.VerifMachine;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using SopalTrace.Domain.Exceptions;

namespace SopalTrace.Application.Services.Execution;

public class VerifMachineExecutionService : IVerifMachineExecutionService
{
    private readonly IOperateurRepository _operateurRepository;
    private readonly IDemarragePosteRepository _postes;

    public VerifMachineExecutionService(IOperateurRepository operateurRepository, IDemarragePosteRepository postes)
    {
        _operateurRepository = operateurRepository;
        _postes = postes;
    }

    public async Task<ExecVerifMachineSessionDto> GetExecVerifMachineAsync(Guid statutId)
    {
        var docStatut = await GetStatutVerifMachineAsync(statutId);
        // Historique en ajout seul : seule la dernière saisie de chaque échéance est affichée.
        var reponses = (await _operateurRepository.GetExecVerifMachineReponsesAsync(statutId))
            .GroupBy(r => r.DocumentVerifMachineEcheanceId)
            .Select(g => g.OrderByDescending(r => r.DateExecution).First())
            .ToList();

        var aujourdhui = DateTime.Today;
        var sessions = await _operateurRepository.GetDocumentStatutsForMachineAsync(docStatut.ExecControleOfId, docStatut.MachineCode!);

        return new ExecVerifMachineSessionDto
        {
            NumeroOf = docStatut.ExecControleOf.NumeroOf,
            CodeArticle = docStatut.ExecControleOf.NumeroOfNavigation?.CodeArticle,
            DesignationArticle = docStatut.ExecControleOf.NumeroOfNavigation?.CodeArticleNavigation?.Designation,
            Equipe = docStatut.Equipe,
            MachineCode = docStatut.MachineCode,
            DateExecution = docStatut.DateExecution ?? docStatut.DateTermine,
            AvailableSessions = sessions.Select(s => new VerifMachineSessionSummaryDto
            {
                StatutId = s.Id,
                Equipe = s.Equipe,
                DateExecution = s.DateExecution,
                DateTermine = s.DateTermine,
                EstTermine = s.EstTermine || (s.DateExecution.HasValue && s.DateExecution.Value.Date < aujourdhui),
                MatriculeOperateur = s.MatriculeOperateur
            }).ToList(),
            Reponses = reponses.Select(r => new ExecVerifMachineReponseDto
            {
                Id = r.Id,
                ExecControleDocumentStatutId = r.ExecControleDocumentStatutId,
                DocumentVerifMachineEcheanceId = r.DocumentVerifMachineEcheanceId,
                DateExecution = r.DateExecution,
                MatriculeOperateur = r.MatriculeOperateur,
                PressionEntree = r.PressionEntree,
                FuiteAffichee = r.FuiteAffichee,
                Conforme = r.Conforme,
                Observation = r.Observation
            }).ToList()
        };
    }

    public async Task SaveExecVerifMachineAsync(SaveExecVerifMachineRequest request)
    {
        var docStatut = await GetStatutVerifMachineAsync(request.ExecControleDocumentStatutId);
        if (docStatut.ExecControleOf.Statut == StatutsExecution.Cloture)
            throw new EtatExecutionInvalideException("Cet OF est clôturé : la vérification machine ne peut plus être modifiée.");

        // Les vérifications d'une équipe qui ne tient plus le poste restent consultables mais ne sont plus modifiables.
        if (docStatut.SessionId is not Guid sessionId || docStatut.PosteCode == null || !await _postes.EstEnCoursAsync(sessionId, docStatut.PosteCode))
            throw new EtatExecutionInvalideException("Cette vérification appartient à une équipe qui ne tient plus ce poste : elle ne peut plus être modifiée.");

        var dateExecution = DateTime.Now;
        docStatut.DateExecution ??= dateExecution;
        docStatut.MatriculeOperateur ??= request.MatriculeOperateur;
        if (docStatut.EstTermine)
        {
            docStatut.EstTermine = false;
            docStatut.DateTermine = null;
        }

        // Les réponses précédentes sont conservées (preuve ISO 9001) : la nouvelle saisie s'ajoute à l'historique.
        _operateurRepository.AddExecVerifMachineReponses(request.Reponses.Select(r => new ExecVerifMachineReponse
        {
            ExecControleDocumentStatutId = docStatut.Id,
            DocumentVerifMachineEcheanceId = r.DocumentVerifMachineEcheanceId,
            DateExecution = dateExecution,
            MatriculeOperateur = request.MatriculeOperateur,
            PressionEntree = r.PressionEntree,
            FuiteAffichee = r.FuiteAffichee,
            Conforme = r.Conforme,
            Observation = r.Observation
        }).ToList());

        await _operateurRepository.SaveChangesAsync();
    }

    public async Task TerminerToutDocumentsMachineAsync(Guid execControleOfId, string machineCode)
    {
        await GetExecutionOuverteAsync(execControleOfId);

        var maintenant = DateTime.Now;
        var statuts = await _operateurRepository.GetDocumentStatutsForMachineAsync(execControleOfId, machineCode);
        foreach (var s in statuts)
        {
            s.EstDemarrageTermine = true;
            if (!s.EstTermine)
            {
                s.EstTermine = true;
                s.DateTermine ??= maintenant;
                s.DateExecution ??= maintenant;
            }
        }
        await _operateurRepository.SaveChangesAsync();
    }

    public async Task CloturerTousVerifMachineAsync(Guid execControleOfId, string posteCode)
    {
        await GetExecutionOuverteAsync(execControleOfId);

        var postes = await _operateurRepository.GetPostesForExecutionAsync(execControleOfId);
        if (!postes.Contains(posteCode))
            throw new RegleMetierException($"Le poste {posteCode} n'est pas affecté à cet OF.");

        var maintenant = DateTime.Now;
        // Vérifications de l'équipe qui tient le poste.
        var session = await _postes.GetSessionAsync(execControleOfId, posteCode)
            ?? throw new EtatExecutionInvalideException($"Aucune équipe ne tient le poste {posteCode}.");
        var statutsPoste = (await _operateurRepository.GetDocumentStatutsAsync(execControleOfId, session.Id))
            .Where(x => x.PosteCode == posteCode)
            .ToList();

        foreach (var s in statutsPoste.Where(x => x.TypeDocument == TypesDocumentExecution.VerifMachine && !x.EstTermine))
        {
            s.EstDemarrageTermine = true;
            s.EstTermine = true;
            s.DateTermine ??= maintenant;
        }

        // Marqueur de clôture rattaché à la session active pour être visible dans la liste des documents de l'équipe.
        if (!statutsPoste.Any(x => x.TypeDocument == TypesDocumentExecution.ClotureVerifMachinePoste))
        {
            _operateurRepository.AddExecControleDocumentStatut(new ExecControleDocumentStatut
            {
                Id = Guid.NewGuid(),
                ExecControleOfId = execControleOfId,
                PosteCode = posteCode,
                TypeDocument = TypesDocumentExecution.ClotureVerifMachinePoste,
                SessionId = session.Id,
                Equipe = session.Equipe,
                DateExecution = maintenant,
                DateTermine = maintenant,
                EstDemarrageTermine = true,
                EstTermine = true
            });
        }

        await _operateurRepository.SaveChangesAsync();
    }

    private async Task<ExecControleDocumentStatut> GetStatutVerifMachineAsync(Guid statutId)
    {
        var statut = await _operateurRepository.GetDocumentStatutByIdAsync(statutId);
        if (statut == null || statut.TypeDocument != TypesDocumentExecution.VerifMachine || string.IsNullOrEmpty(statut.MachineCode))
            throw new RessourceIntrouvableException("Document de vérification machine introuvable.");
        return statut;
    }

    private async Task GetExecutionOuverteAsync(Guid execControleOfId)
    {
        var execOf = await _operateurRepository.GetExecOfByIdAsync(execControleOfId)
            ?? throw new RessourceIntrouvableException("Exécution introuvable.");

        if (execOf.Statut == StatutsExecution.Cloture)
            throw new EtatExecutionInvalideException("Cet OF est clôturé : aucune modification n'est possible.");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.Dtos.VerifMachine;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services.Execution;

public class VerifMachineExecutionService : IVerifMachineExecutionService
{
    private readonly IOperateurRepository _operateurRepository;

    public VerifMachineExecutionService(IOperateurRepository operateurRepository)
    {
        _operateurRepository = operateurRepository;
    }

    public async Task<ExecVerifMachineSessionDto> GetExecVerifMachineAsync(Guid statutId, Guid? periodiciteId = null)
    {
        var reponses = await _operateurRepository.GetExecVerifMachineReponsesAsync(statutId, periodiciteId);
        var docStatut = await _operateurRepository.GetDocumentStatutByIdAsync(statutId);

        var availableSessions = new List<VerifMachineSessionSummaryDto>();
        if (docStatut != null && !string.IsNullOrEmpty(docStatut.MachineCode))
        {
            var today = DateTime.Now.Date;
            var sessions = await _operateurRepository.GetDocumentStatutsForMachineAsync(docStatut.ExecControleOfId, docStatut.MachineCode);
            availableSessions = sessions.Select(s => new VerifMachineSessionSummaryDto
            {
                StatutId = s.Id,
                Equipe = s.Equipe,
                DateExecution = s.DateExecution,
                DateTermine = s.DateTermine,
                EstTermine = s.EstTermine || (s.DateExecution.HasValue && s.DateExecution.Value.Date < today),
                MatriculeOperateur = s.MatriculeOperateur
            }).ToList();
        }

        return new ExecVerifMachineSessionDto
        {
            NumeroOf = docStatut?.ExecControleOf?.NumeroOf,
            CodeArticle = docStatut?.ExecControleOf?.NumeroOfNavigation?.CodeArticle,
            DesignationArticle = docStatut?.ExecControleOf?.NumeroOfNavigation?.CodeArticleNavigation?.Designation,
            Equipe = docStatut?.Equipe,
            MachineCode = docStatut?.MachineCode,
            DateExecution = docStatut?.DateExecution ?? docStatut?.DateTermine,
            AvailableSessions = availableSessions,
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

    public async Task<bool> SaveExecVerifMachineAsync(SaveExecVerifMachineRequest request)
    {
        var dateExecution = DateTime.Now;

        var docStatut = await _operateurRepository.GetDocumentStatutByIdAsync(request.ExecControleDocumentStatutId);
        if (docStatut != null)
        {
            docStatut.DateExecution ??= dateExecution;
            docStatut.MatriculeOperateur ??= request.MatriculeOperateur;
            if (docStatut.EstTermine)
            {
                docStatut.EstTermine = false;
                docStatut.DateTermine = null;
            }
            _operateurRepository.UpdateExecControleDocumentStatut(docStatut);
        }

        var anciennesReponses = await _operateurRepository.GetExecVerifMachineReponsesAsync(request.ExecControleDocumentStatutId, request.PeriodiciteMachineId);
        
        if (anciennesReponses.Any())
        {
            _operateurRepository.RemoveExecVerifMachineReponses(anciennesReponses);
        }

        var nouvellesReponses = request.Reponses.Select(r => new ExecVerifMachineReponse
        {
            ExecControleDocumentStatutId = request.ExecControleDocumentStatutId,
            DocumentVerifMachineEcheanceId = r.DocumentVerifMachineEcheanceId,
            DateExecution = dateExecution,
            MatriculeOperateur = request.MatriculeOperateur,
            PressionEntree = r.PressionEntree,
            FuiteAffichee = r.FuiteAffichee,
            Conforme = r.Conforme,
            Observation = r.Observation
        }).ToList();

        _operateurRepository.AddExecVerifMachineReponses(nouvellesReponses);

        await _operateurRepository.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<object>> GetPeriodicitesMachineAsync()
    {
        var periodes = await _operateurRepository.GetPeriodicitesMachineAsync();
        return periodes.Select(p => new { p.Id, p.Code, p.Libelle, p.OrdreAffichage });
    }

    public async Task<bool> TerminerToutDocumentsMachineAsync(Guid execControleOfId, string machineCode)
    {
        var statuts = await _operateurRepository.GetDocumentStatutsForMachineAsync(execControleOfId, machineCode);
        var now = DateTime.Now;
        foreach (var s in statuts)
        {
            s.EstDemarrageTermine = true;
            if (!s.EstTermine)
            {
                s.EstTermine = true;
                s.DateTermine ??= now;
                s.DateExecution ??= now;
                _operateurRepository.UpdateExecControleDocumentStatut(s);
            }
        }
        await _operateurRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CloturerTousVerifMachineAsync(Guid execControleOfId, string posteCode)
    {
        var clotureDoc = new ExecControleDocumentStatut
        {
            Id = Guid.NewGuid(),
            ExecControleOfId = execControleOfId,
            PosteCode = posteCode,
            TypeDocument = "CLOTURE_VM_POSTE",
            DateExecution = DateTime.Now,
            EstDemarrageTermine = true,
            EstTermine = true
        };
        _operateurRepository.AddExecControleDocumentStatut(clotureDoc);
        
        var statuts = await _operateurRepository.GetDocumentStatutsAsync(execControleOfId);
        foreach (var s in statuts.Where(x => x.PosteCode == posteCode && x.TypeDocument == "VERIF_MACHINE" && !x.EstTermine))
        {
            s.EstDemarrageTermine = true;
            s.EstTermine = true;
            s.DateTermine ??= DateTime.Now;
            _operateurRepository.UpdateExecControleDocumentStatut(s);
        }
        
        await _operateurRepository.SaveChangesAsync();
        return true;
    }
}

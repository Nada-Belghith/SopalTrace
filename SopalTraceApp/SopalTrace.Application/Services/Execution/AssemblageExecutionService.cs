using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Application.Interfaces;
using SopalTrace.Application.Interfaces.Execution;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services.Execution;

public class AssemblageExecutionService : IAssemblageExecutionService
{
    private readonly IOperateurRepository _operateurRepository;
    private readonly IOccurrenceService _occurrenceService;
    private readonly IOccurrenceRepository _occurrenceRepository;

    public AssemblageExecutionService(
        IOperateurRepository operateurRepository,
        IOccurrenceService occurrenceService,
        IOccurrenceRepository occurrenceRepository)
    {
        _operateurRepository = operateurRepository;
        _occurrenceService = occurrenceService;
        _occurrenceRepository = occurrenceRepository;
    }

    public async Task<ExecControleOfDto> DemarrerOfAssemblageAsync(DemarrerOfAssemblageRequest request)
    {
        var of = await _operateurRepository.GetOfAsync(request.NumeroOf);
        if (of == null) throw new Exception("OF introuvable");

        var planAss = await _operateurRepository.GetPlanAssemblageActifAsync(of.CodeArticle);
        
        var execOf = new ExecControleOf
        {
            NumeroOf = request.NumeroOf,
            OperationCode = request.OperationCode,
            PlanSourceId = null,
            TypeOf = "ASS",
            Statut = "EN_COURS",
            DateDebut = DateTime.Now
        };

        _operateurRepository.AddExecControleOf(execOf);
        await _operateurRepository.SaveChangesAsync();

        foreach (var posteCode in request.PosteCodes)
        {
            var execPoste = new ExecControleOfPoste
            {
                ExecControleOfId = execOf.Id,
                PosteCode = posteCode
            };
            execOf.ExecControleOfPostes.Add(execPoste);
        }

        await _operateurRepository.SaveChangesAsync();

        execOf.Statut = "EN_COURS";
        execOf.EstEnReglage = false;
        await _operateurRepository.SaveChangesAsync();
        await _occurrenceService.GenererOccurrencesInitialesAsync(execOf.Id);

        return new ExecControleOfDto
        {
            Id = execOf.Id,
            NumeroOf = execOf.NumeroOf,
            OperationCode = execOf.OperationCode,
            Statut = execOf.Statut,
            DateDebut = execOf.DateDebut
        };
    }

    public async Task<bool> InitDocumentsAsync(Guid execControleOfId, string typeDocument, string? posteCode, string? machineCode = null, string? equipe = null, string? matricule = null)
    {
        var execOf = await _operateurRepository.GetExecOfByIdAsync(execControleOfId);
        if (execOf == null) throw new Exception("Exécution introuvable");

        var postes = await _operateurRepository.GetPostesForExecutionAsync(execControleOfId);
        var existing = await _operateurRepository.GetDocumentStatutsAsync(execControleOfId);
        var today = DateTime.Now.Date;
        
        if (existing.Any(s => s.TypeDocument == typeDocument && (posteCode == null || s.PosteCode == posteCode) && (machineCode == null || s.MachineCode == machineCode) && s.Equipe == equipe && s.DateExecution == today)) return true;

        bool initializedAny = false;

        if (typeDocument == "VERIF_MACHINE" || typeDocument == "RESULTAT_CONTROLE_POSTE" || execOf.TypeOf == "ASS")
        {
            var of = await _operateurRepository.GetOfAsync(execOf.NumeroOf);
            var postesToInit = !string.IsNullOrEmpty(posteCode) ? new List<string> { posteCode } : postes.ToList();

            foreach (var p in postesToInit)
            {
                IEnumerable<RefFormulaire> forms = Enumerable.Empty<RefFormulaire>();
                if (typeDocument == "VERIF_MACHINE")
                {
                    if (!string.IsNullOrEmpty(machineCode))
                    {
                        forms = await _operateurRepository.GetFormulairesPourMachineAsync(machineCode, typeDocument);
                    }
                }
                else if (typeDocument == "RESULTAT_CONTROLE_POSTE")
                {
                    var plan = await _operateurRepository.GetPlanControlePosteAsync(p, of?.CodeArticle ?? "");
                    if (plan != null)
                    {
                        forms = new List<RefFormulaire> { new RefFormulaire { Id = plan.Id } };
                    }
                }
                else
                {
                    forms = of != null ? await _operateurRepository.GetFormulairesPourArticleAsync(of.CodeArticle, typeDocument) : Enumerable.Empty<RefFormulaire>();
                }

                foreach (var f in forms)
                {
                    _operateurRepository.AddExecControleDocumentStatut(new ExecControleDocumentStatut
                    {
                        ExecControleOfId = execControleOfId,
                        PosteCode = p,
                        MachineCode = machineCode,
                        TypeDocument = typeDocument,
                        DocId = f.Id,
                        EstTermine = false,
                        Equipe = equipe,
                        DateExecution = today,
                        MatriculeOperateur = matricule
                    });
                    initializedAny = true;
                }
            }
        }
        else
        {
            var of = await _operateurRepository.GetOfAsync(execOf.NumeroOf);
            if (of != null)
            {
                var forms = await _operateurRepository.GetFormulairesPourArticleAsync(of.CodeArticle, typeDocument);
                foreach (var f in forms)
                {
                    _operateurRepository.AddExecControleDocumentStatut(new ExecControleDocumentStatut
                    {
                        ExecControleOfId = execControleOfId,
                        PosteCode = null,
                        TypeDocument = typeDocument,
                        DocId = f.Id,
                        EstTermine = false,
                        Equipe = equipe,
                        DateExecution = today,
                        MatriculeOperateur = matricule
                    });
                    initializedAny = true;
                }
            }
        }

        if (initializedAny)
        {
            await _operateurRepository.SaveChangesAsync();
        }

        return initializedAny;
    }

    public async Task<IEnumerable<DocumentStatutDto>> GetDocumentsAssemblageStatusAsync(Guid execControleOfId)
    {
        var statuts = (await _operateurRepository.GetDocumentStatutsAsync(execControleOfId)).ToList();

        var today = DateTime.Now.Date;
        bool hasChanges = false;
        foreach (var s in statuts)
        {
            if (!s.EstTermine && s.DateExecution.HasValue && s.DateExecution.Value.Date < today)
            {
                s.EstTermine = true;
                s.DateTermine = s.DateExecution.Value.Date.AddDays(1).AddTicks(-1);
                _operateurRepository.UpdateExecControleDocumentStatut(s);
                hasChanges = true;
            }

            if ((s.TypeDocument == "PLAN_ASS" || s.TypeDocument == "PLAN_ASSEMBLAGE") && !s.DocId.HasValue)
            {
                var execOf = await _operateurRepository.GetExecOfByIdAsync(execControleOfId);
                var of = execOf != null ? await _operateurRepository.GetOfAsync(execOf.NumeroOf) : null;
                if (of != null)
                {
                    var planAss = await _operateurRepository.GetPlanAssemblageActifAsync(of.CodeArticle);
                    if (planAss != null)
                    {
                        s.DocId = planAss.Id;
                        _operateurRepository.UpdateExecControleDocumentStatut(s);
                        hasChanges = true;
                    }
                }
            }
        }
        
        if (hasChanges)
        {
            await _operateurRepository.SaveChangesAsync();
        }

        var formIds = statuts.Where(s => s.DocId.HasValue && s.TypeDocument != "ECHANTILLONNAGE").Select(s => s.DocId.GetValueOrDefault()).Distinct();
        var designations = await _operateurRepository.GetFormulaireDesignationsAsync(formIds);

        var machineCodes = statuts.Where(s => !string.IsNullOrEmpty(s.MachineCode)).Select(s => s.MachineCode!).Distinct().ToList();
        var machineNames = await _operateurRepository.GetMachineLabelsAsync(machineCodes);

        return statuts.Select(s => new DocumentStatutDto
        {
            Id = s.Id,
            TypeDocument = s.TypeDocument,
            PosteCode = s.PosteCode,
            MachineCode = s.MachineCode,
            MachineLibelle = !string.IsNullOrEmpty(s.MachineCode) && machineNames.ContainsKey(s.MachineCode) ? machineNames[s.MachineCode] : null,
            DocId = s.DocId,
            LibelleFormulaire = s.TypeDocument == "ECHANTILLONNAGE" ? "Document Échantillonnage" : (s.DocId.HasValue && designations.ContainsKey(s.DocId.Value) ? designations[s.DocId.Value] : null),
            EstDemarrageTermine = s.EstDemarrageTermine,
            EstPauseTermine = s.EstPauseTermine,
            EstTermine = s.EstTermine,
            DateTermine = s.DateTermine,
            Equipe = s.Equipe,
            DateExecution = s.DateExecution
        });
    }

    public async Task<IEnumerable<MachineDto>> GetMachinesByPosteAsync(string posteCode)
    {
        var machines = await _operateurRepository.GetMachinesByPosteAsync(posteCode);
        var periodicitesMap = await _operateurRepository.GetMachinesPlanPeriodicitesAsync();
        return machines.Select(m => {
            var per = periodicitesMap.ContainsKey(m.CodeMachine) ? periodicitesMap[m.CodeMachine] : new List<string>();
            return new MachineDto { 
                CodeMachine = m.CodeMachine, 
                Libelle = m.Libelle, 
                HasDemarragePlan = per.Any(p => p.Contains("démarrage") || p.Contains("demarrage") || p.Contains("début") || p.Contains("debut")),
                HasApresPausePlan = per.Any(p => p.Contains("après la pause") || p.Contains("apres la pause") || p.Contains("après pause") || p.Contains("apres pause") || p.Contains("pause")),
                HasFinPostePlan = per.Any(p => p.Contains("fin de poste") || p.Contains("fin poste") || p.Contains("fin du poste") || p.Contains("fin"))
            };
        });
    }

    public async Task<bool> MarquerDocumentTermineAsync(Guid statutId, string? matricule = null, string periodicite = "")
    {
        var statut = await _operateurRepository.GetDocumentStatutByIdAsync(statutId);
        if (statut == null) return false;

        bool hasApresPause = false;
        bool hasFinPoste = false;
        
        if (!string.IsNullOrEmpty(statut.MachineCode))
        {
            var periodicitesMap = await _operateurRepository.GetMachinesPlanPeriodicitesAsync();
            if (periodicitesMap.ContainsKey(statut.MachineCode))
            {
                var per = periodicitesMap[statut.MachineCode];
                hasApresPause = per.Any(p => p.Contains("après la pause") || p.Contains("apres la pause") || p.Contains("après pause") || p.Contains("apres pause") || p.Contains("pause"));
                hasFinPoste = per.Any(p => p.Contains("fin de poste") || p.Contains("fin poste") || p.Contains("fin du poste") || p.Contains("fin"));
            }
        }

        if (periodicite == "APRES_PAUSE")
        {
            statut.EstPauseTermine = true;
            if (!hasFinPoste) statut.EstTermine = true;
        }
        else if (periodicite == "FIN_POSTE" || periodicite == "fin_poste")
        {
            statut.EstTermine = true;
        }
        else
        {
            statut.EstDemarrageTermine = true;
            if (!hasApresPause && !hasFinPoste) statut.EstTermine = true;
        }

        statut.DateTermine = DateTime.Now;
        if (!string.IsNullOrEmpty(matricule))
        {
            statut.MatriculeOperateur = matricule;
        }

        await _operateurRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RouvrirDocumentAsync(Guid statutId, string periodicite = "")
    {
        var statut = await _operateurRepository.GetDocumentStatutByIdAsync(statutId);
        if (statut == null) return false;

        if (periodicite == "APRES_PAUSE")
        {
            statut.EstPauseTermine = false;
        }
        else if (periodicite == "FIN_POSTE" || periodicite == "fin_poste")
        {
            statut.EstTermine = false;
        }
        else
        {
            statut.EstDemarrageTermine = false;
            statut.EstTermine = false;
        }
        statut.DateTermine = null;

        await _operateurRepository.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<OfAssemblageStatutDto>> GetOfsAssemblageStatutAsync()
    {
        var allOfs = await _operateurRepository.GetAllOfOperationsDisponiblesAsync();
        var ofsAss = allOfs.Where(o => o.GammeOperatoire.Any(g => g.OperationCode == "ASS")).ToList();

        var execsEnCours = (await _operateurRepository.GetExecsAssemblageEnCoursAsync()).ToList();

        var result = new List<OfAssemblageStatutDto>();

        foreach (var of in ofsAss)
        {
            var execExistante = execsEnCours.FirstOrDefault(e => e.NumeroOf == of.NumeroOf);

            result.Add(new OfAssemblageStatutDto
            {
                NumeroOf = of.NumeroOf,
                DesignationArticle = of.DesignationArticle,
                CodeArticle = of.CodeArticle,
                QuantiteLancee = of.QuantiteLancee,
                DateDebut = execExistante != null ? execExistante.DateDebut : of.DateDebut,
                Statut = execExistante != null ? execExistante.Statut : null,
                ExecControleOfId = execExistante?.Id,
                PostesExistants = execExistante?.ExecControleOfPostes.Select(p => p.PosteCode).ToList() ?? new List<string>()
            });
        }

        return result;
    }

    public async Task<bool> AjouterPostesAsync(Guid execControleOfId, List<string> posteCodes)
    {
        var execOf = await _operateurRepository.GetExecOfByIdAsync(execControleOfId);
        if (execOf == null) return false;

        var postesExistants = (await _operateurRepository.GetPostesForExecutionAsync(execControleOfId)).ToHashSet();

        foreach (var poste in posteCodes.Distinct())
        {
            if (!postesExistants.Contains(poste))
            {
                _operateurRepository.AddExecControleOfPoste(new ExecControleOfPoste
                {
                    ExecControleOfId = execControleOfId,
                    PosteCode = poste
                });
            }
        }

        await _operateurRepository.SaveChangesAsync();
        return true;
    }
}

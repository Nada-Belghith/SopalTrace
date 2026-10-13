using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SopalTrace.Application.DTOs.Execution.Operateur;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Execution;

public interface IOperateurRepository
{
    // --- OF & exécution ---
    Task<MfgheadOrdreFabrication?> GetOfAsync(string numeroOf);
    Task<Operation?> GetOperationAsync(string operationCode);
    Task<DocumentEntete?> GetPlanActifAsync(string codeArticle, string? operationCode = null);

    /// <summary>Cotes de tronçonnage saisies au démarrage du dernier OF de l'article pour cette opération.</summary>
    Task<(decimal? Longueur, decimal? Diametre)> GetDernieresCotesTronconnageAsync(string codeArticle, string? operationCode);
    Task<ExecControleOf?> GetExecOfByIdAsync(Guid execOfId);
    Task<ExecControleOf?> GetExecOfWithIntermediairesAsync(Guid execOfId);
    Task<IEnumerable<string>> GetPostesForExecutionAsync(Guid execOfId);
    Task<bool> HasReglageSectionsAsync(Guid planSourceId);
    void AddExecControleOf(ExecControleOf execOf);
    Task<bool> CanStartOperationAsync(string numeroOf, string operationCode);
    Task SaveChangesAsync();

    // --- Catalogue ---
    Task<IEnumerable<PosteTravail>> GetPostesDisponiblesAsync();
    Task<PosteTravail?> GetPosteWithMachinesAsync(string posteCode);
    Task<IReadOnlySet<string>> GetCodesPostesActifsAsync(IEnumerable<string> posteCodes);
    Task<IEnumerable<OperateurOfDto>> GetAllOfOperationsDisponiblesAsync();
    Task<IEnumerable<Machine>> GetAllMachinesAsync();
    Task<Dictionary<string, string>> GetMachineLabelsAsync(IEnumerable<string> machineCodes);
    /// <summary>Moments de contrôle (PeriodiciteMachine.ControlMoment) des plans actifs, par machine.</summary>
    Task<Dictionary<string, List<string>>> GetMachinesPlanPeriodicitesAsync();
    /// <summary>Moments de contrôle du plan actif de la machine (DEMARRAGE, APRES_PAUSE, FIN_POSTE).</summary>
    Task<List<string>> GetPeriodicitesPlanMachineAsync(string machineCode);

    // --- Assemblage (OF produits finis) ---
    Task<List<OfAssemblageStatutDto>> GetOfsAssemblageStatutAsync();
    Task<bool> OfAssemblageDisponibleAsync(string numeroOf);
    Task<bool> ExisteExecutionAssemblageActiveAsync(string numeroOf);
    Task<Guid?> GetPlanVerifMachineActifIdAsync(string machineCode);

    /// <summary>Échéances d'un plan de vérification machine avec le moment de contrôle de leur périodicité (vide si aucun).</summary>
    Task<List<(Guid EcheanceId, string Periodicite)>> GetEcheancesPlanVerifMachineAsync(Guid planId);

    /// <summary>Statuts de vérification machine d'un OF (toutes sessions), pour l'historique.</summary>
    Task<List<ExecControleDocumentStatut>> GetStatutsVerifMachineAsync(Guid execControleOfId);
    Task<Dictionary<Guid, string>> GetFormulaireDesignationsAsync(IEnumerable<Guid> ids);
    void AddExecControleOfPoste(ExecControleOfPoste poste);

    // --- Sessions d'équipe ---
    void AddSession(ExecControleOfSession session);

    // --- Statuts de documents ---
    /// <summary>Statuts de documents de la session donnée.</summary>
    Task<IEnumerable<ExecControleDocumentStatut>> GetDocumentStatutsAsync(Guid execControleOfId, Guid sessionId);
    /// <summary>Statuts de documents de l'OF, toutes sessions (suivis).</summary>
    Task<List<ExecControleDocumentStatut>> GetStatutsOfAsync(Guid execControleOfId);
    Task<ExecControleDocumentStatut?> GetDocumentStatutByIdAsync(Guid statutId);
    Task<List<ExecControleDocumentStatut>> GetDocumentStatutsForMachineAsync(Guid execControleOfId, string machineCode);
    void AddExecControleDocumentStatut(ExecControleDocumentStatut statut);
    void UpdateExecControleDocumentStatut(ExecControleDocumentStatut statut);

    // --- Vérification machine ---
    void AddExecVerifMachineReponses(IEnumerable<ExecVerifMachineReponse> reponses);
    Task<List<ExecVerifMachineReponse>> GetExecVerifMachineReponsesAsync(Guid statutId);
}

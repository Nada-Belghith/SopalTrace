using SopalTrace.Application.DTOs.Magasin;

namespace SopalTrace.Application.Interfaces;

public interface IMagasinService
{
    Task<PreparationDto> GetOfAsync(string scanOuNumero);
    Task<PreparationDto> OuvrirPreparationAsync(string scanOuNumero);
    Task<ScanPreparationDto> ScannerComposantAsync(Guid preparationId, string contenu);
    Task<PreparationDto> RetirerLotAsync(Guid preparationId, Guid lotId);
    Task<PreparationDto> TerminerPreparationAsync(Guid preparationId, string? motif);
    Task<PreparationDto> AnnulerPreparationAsync(Guid preparationId);

    Task<ExpeditionDto> GetBlAsync(string scanOuNumero);
    Task<ExpeditionDto> OuvrirExpeditionAsync(string scanOuNumero);
    Task<ExpeditionDto> ScannerOfAsync(Guid expeditionId, string contenu);
    Task<ExpeditionDto> RetirerOfAsync(Guid expeditionId, Guid scanId);
    Task<ExpeditionDto> TerminerExpeditionAsync(Guid expeditionId);
}

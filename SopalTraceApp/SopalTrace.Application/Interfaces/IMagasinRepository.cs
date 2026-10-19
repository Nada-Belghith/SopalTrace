using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces;

public interface IMagasinRepository
{
    /// <summary>OF avec son article (et sa nature) et ses besoins (nomenclature) avec leurs articles.</summary>
    Task<MfgheadOrdreFabrication?> GetOfAvecNomenclatureAsync(string numeroOf);

    /// <summary>Préparation non annulée de l'OF, avec ses lots.</summary>
    Task<MagPreparationOf?> GetPreparationActiveAsync(string numeroOf);
    Task<MagPreparationOf?> GetPreparationAsync(Guid id);
    void AjouterPreparation(MagPreparationOf preparation);
    void AjouterLot(MagPreparationOfLot lot);
    void SupprimerLot(MagPreparationOfLot lot);

    Task<Sdelivery?> GetBlAsync(string numeroBl);
    /// <summary>Expédition du BL avec ses OF (et leurs articles).</summary>
    Task<MagExpeditionBl?> GetExpeditionDuBlAsync(string numeroBl);
    Task<MagExpeditionBl?> GetExpeditionAsync(Guid id);
    void AjouterExpedition(MagExpeditionBl expedition);
    void AjouterOfExpedie(MagExpeditionBlScanOf scan);
    void SupprimerOfExpedie(MagExpeditionBlScanOf scan);

    /// <summary>Toutes les opérations de la gamme de l'OF ont une exécution clôturée et aucune n'est encore ouverte.</summary>
    Task<bool> ProductionTermineeAsync(string numeroOf);

    Task SaveChangesAsync();
}

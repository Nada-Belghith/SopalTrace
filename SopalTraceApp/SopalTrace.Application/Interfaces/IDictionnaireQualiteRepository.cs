using System;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces;

public interface IDictionnaireQualiteRepository
{
    Task<Periodicite> GetPeriodiciteByLibelleAsync(string libelle);
    Task<TypeSection> GetTypeSectionByLibelleAsync(string libelle);
    Task AddTypeSectionAsync(TypeSection entite);
    
    Task<TypeCaracteristique> GetTypeCaracteristiqueByLibelleAsync(string libelle);
    Task AddTypeCaracteristiqueAsync(TypeCaracteristique entite);

    Task<TypeControle> GetTypeControleByLibelleAsync(string libelle);
    Task AddTypeControleAsync(TypeControle entite);

    Task<MoyenControle> GetMoyenControleByLibelleAsync(string libelle);
    Task AddMoyenControleAsync(MoyenControle entite);
}

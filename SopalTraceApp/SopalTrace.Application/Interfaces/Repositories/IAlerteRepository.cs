using System;
using System.Threading.Tasks;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Interfaces.Repositories;

public interface IAlerteRepository
{
    Task<Alerte?> GetByIdAsync(Guid id);
    Task AddAsync(Alerte alerte);
    Task UpdateAsync(Alerte alerte);
    Task<bool> ExisteNonResolueDepuisAsync(string typeAlerte, string cleEntite, DateTime depuis);
    Task<System.Collections.Generic.List<Alerte>> GetNonResoluesParArticleAsync(string articleCode);
    Task ResoudreAlertesPourEntiteAsync(string cleEntite, string resoluPar);
}

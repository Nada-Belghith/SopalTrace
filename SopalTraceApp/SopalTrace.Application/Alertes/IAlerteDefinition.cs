using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Alertes;

public interface IAlerteDefinition<TContexte>
{
    string TypeAlerte { get; }
    TimeSpan FenetreAntiSpam { get; }
    
    Task<List<string>> ResoudreDestinatairesAsync(TContexte contexte);
    string ConstruireSujet(TContexte contexte);
    string ConstruireCorps(TContexte contexte);
    string ExtraireCleEntite(TContexte contexte);
}

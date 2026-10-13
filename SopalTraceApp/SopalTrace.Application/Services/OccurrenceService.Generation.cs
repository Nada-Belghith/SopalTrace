using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Services;

public partial class OccurrenceService
{
    public async Task GenererOccurrencesReglageCoursAsync(Guid execControleOfId)
    {
        var of = await _occurrenceRepository.GetExecControleOfWithIntermediairesAsync(execControleOfId);
        if (of == null) return;

        string trancheReglage = $"REGLAGE_COURS_{DateTime.Now:HH_mm}";
        var newIntermediaires = new List<ExecPrelevementIntermediaire>();
        
        // Réglage de fabrication : pas d'équipe (les réglages d'assemblage sont suivis par la planification, par poste).
        Guid? sessionId = null;

        if (of.PlanSourceId.HasValue)
        {
            var sectionsActives = await _occurrenceRepository.GetSectionsActivesAsync(of.PlanSourceId.Value);
            newIntermediaires.AddRange(sectionsActives
                .Where(s => s.TypeSection?.TypeNormalise == SectionTypes.Reglage || s.TypeSection?.TypeNormalise == SectionTypes.ReglageProd)
                .Select(s => CreerIntermediaire(of.Id, s.Id, trancheReglage, 0, DateTime.Now, sessionId)));
        }

        if (of.TypeOf == TypesOf.Assemblage)
        {
            var assSections = await _occurrenceRepository.GetDocumentSectionsActivesAsync(execControleOfId);
            newIntermediaires.AddRange(assSections
                .Where(s => (s.TypeSection?.TypeNormalise == SectionTypes.Reglage || s.TypeSection?.TypeNormalise == SectionTypes.ReglageProd) || 
                            (s.LibelleSection != null && (s.LibelleSection.Contains("réglage", StringComparison.OrdinalIgnoreCase) || s.LibelleSection.Contains("reglage", StringComparison.OrdinalIgnoreCase))))
                .Select(s => CreerIntermediaire(of.Id, s.Id, trancheReglage, 0, DateTime.Now, sessionId)));
        }

        if (newIntermediaires.Any())
        {
            _occurrenceRepository.AddIntermediaires(newIntermediaires);
            await _occurrenceRepository.SaveChangesAsync();
        }
    }

    public async Task NettoyerOccurrencesReglageAsync(Guid execControleOfId)
    {
        var of = await _occurrenceRepository.GetExecControleOfWithIntermediairesAsync(execControleOfId);
        if (of == null) return;

        var reglageOccurrences = of.ExecPrelevementIntermediaires
            .Where(o => o.TrancheHoraire.StartsWith("REGLAGE")).ToList();

        if (reglageOccurrences.Any())
        {
            _occurrenceRepository.RemoveIntermediaires(reglageOccurrences);
            await _occurrenceRepository.SaveChangesAsync();
        }
    }


}

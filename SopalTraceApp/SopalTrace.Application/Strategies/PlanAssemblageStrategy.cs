using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using System.Threading.Tasks;

namespace SopalTrace.Application.Strategies;

public class PlanAssemblageStrategy : IDocumentTypeStrategy
{
    public string DocumentTypeCode => "PLAN_ASS";

    public string GetDefaultFormulaireCodeRef(DocumentContextInfo context)
    {
        if (!string.IsNullOrWhiteSpace(context.NatureArticleCode))
            return $"FE-ASS-{context.NatureArticleCode.Trim()}";
        
        return string.Empty;
    }

    public string GetDefaultFormRole() => "UNKNOWN";

    public void ApplyCustomProperties(DocumentEntete document, string? configurationColonnesJson)
    {
        // PLAN_ASS does not store configurationColonnesJson on the document entity itself.
    }

    public Task PopulateDtoAsync(DocumentEntete document, SopalTrace.Application.DTOs.QualityPlans.Documents.DocumentEnteteDto dto)
    {
        return Task.CompletedTask;
    }
}

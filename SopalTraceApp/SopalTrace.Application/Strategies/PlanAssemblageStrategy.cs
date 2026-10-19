using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using System.Threading.Tasks;

namespace SopalTrace.Application.Strategies;

public class PlanAssemblageStrategy : IDocumentTypeStrategy
{
    public string DocumentTypeCode => "PLAN_ASS";



    public Task PopulateDtoAsync(DocumentEntete document, SopalTrace.Application.DTOs.QualityPlans.Documents.DocumentEnteteDto dto)
    {
        return Task.CompletedTask;
    }
}

using SopalTrace.Domain.Entities;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public interface IDocumentTypeStrategy
{
    string DocumentTypeCode { get; }
    
    
    /// <summary>
    /// Populates custom properties into the DocumentEnteteDto when retrieving the document.
    /// </summary>
    Task PopulateDtoAsync(DocumentEntete document, SopalTrace.Application.DTOs.QualityPlans.Documents.DocumentEnteteDto dto);
}

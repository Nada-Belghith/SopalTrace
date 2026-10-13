using SopalTrace.Domain.Entities;
using System.Threading.Tasks;

namespace SopalTrace.Application.Interfaces;

public record DocumentContextInfo(string? PosteCode, string? FamilleProduitFiniCode, string? NatureArticleCode);

public interface IDocumentTypeStrategy
{
    string DocumentTypeCode { get; }
    
    /// <summary>
    /// Gets the default form reference code if not provided by the client.
    /// </summary>
    string GetDefaultFormulaireCodeRef(DocumentContextInfo context);
    
    
    /// <summary>
    /// Populates custom properties into the DocumentEnteteDto when retrieving the document.
    /// </summary>
    Task PopulateDtoAsync(DocumentEntete document, SopalTrace.Application.DTOs.QualityPlans.Documents.DocumentEnteteDto dto);
}

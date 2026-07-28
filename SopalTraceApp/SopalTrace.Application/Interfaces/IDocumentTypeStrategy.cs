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
    /// Gets the default role to assign to the form if it does not have one.
    /// </summary>
    string GetDefaultFormRole();
    
    /// <summary>
    /// Applies custom properties to the DocumentEntete specific to the document type.
    /// </summary>
    void ApplyCustomProperties(DocumentEntete document, string? configurationColonnesJson);
    
    /// <summary>
    /// Populates custom properties into the DocumentEnteteDto when retrieving the document.
    /// </summary>
    Task PopulateDtoAsync(DocumentEntete document, SopalTrace.Application.DTOs.QualityPlans.Documents.DocumentEnteteDto dto);
}

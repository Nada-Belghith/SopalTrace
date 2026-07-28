using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using System.Threading.Tasks;
using System.Linq;

namespace SopalTrace.Application.Strategies;

public class ControlePosteStrategy : IDocumentTypeStrategy
{
    private readonly SopalTrace.Application.Interfaces.IUnitOfWork _unitOfWork;

    public ControlePosteStrategy(SopalTrace.Application.Interfaces.IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public string DocumentTypeCode => "CTRL_POSTE";

    public string GetDefaultFormulaireCodeRef(DocumentContextInfo context)
    {
        if (!string.IsNullOrWhiteSpace(context.PosteCode))
            return $"FE-RC-{context.PosteCode.Trim()}";
        
        return string.Empty;
    }

    public string GetDefaultFormRole() => "RESULTAT_CONTROLE_POSTE";

    public void ApplyCustomProperties(DocumentEntete document, string? configurationColonnesJson)
    {
        if (configurationColonnesJson != null)
        {
            document.Libre3 = configurationColonnesJson;
        }
    }

    public async Task PopulateDtoAsync(DocumentEntete document, SopalTrace.Application.DTOs.QualityPlans.Documents.DocumentEnteteDto dto)
    {
        List<SopalTrace.Domain.Entities.RefFormulaireColonneDef> cols = new();
        List<SopalTrace.Domain.Entities.RefFormulaireEquipe> equipes = new();

        if (document.FormulaireId.HasValue)
        {
            cols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(document.FormulaireId.Value);
            equipes = await _unitOfWork.RefFormulaireRepository.GetEquipesActivesByFormulaireIdAsync(document.FormulaireId.Value);
        }
        else
        {
            var form = await _unitOfWork.RefFormulaireRepository.GetFormulaireActifByCodeReferenceAsync(document.TypeDocumentCode);
            if (form != null)
            {
                cols = await _unitOfWork.RefFormulaireRepository.GetColonnesActivesByFormulaireIdAsync(form.Id);
                equipes = await _unitOfWork.RefFormulaireRepository.GetEquipesActivesByFormulaireIdAsync(form.Id);
            }
        }
        
        var eqList = equipes.Select(e => new { nom = e.NomEquipe, debut = e.HeureDebut, fin = e.HeureFin }).ToList();
        var cList = cols.Select(c => new { key = c.CleColonne, label = c.LabelAffiche, type = c.TypeValeur, insertAfter = c.InsertAfter, targetTable = c.TargetTable }).ToList();
        
        dto.ConfigurationColonnesJson = System.Text.Json.JsonSerializer.Serialize(new { equipes = eqList, customCols = cList }, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
    }
}

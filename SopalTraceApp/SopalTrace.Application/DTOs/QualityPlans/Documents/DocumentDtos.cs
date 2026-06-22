using System;
using System.Collections.Generic;

namespace SopalTrace.Application.DTOs.QualityPlans.Documents;

public class DocumentEnteteDto
{
    public Guid Id { get; set; }
    public string TypeDocumentCode { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public int Version { get; set; }
    public string Statut { get; set; } = string.Empty;
    public string? OperationCode { get; set; }
    public string? OperationLibelle { get; set; }
    public Guid? FormulaireId { get; set; }
    public string? FormulaireCodeReference { get; set; }
    public string? LegendeMoyens { get; set; }
    public string? Remarques { get; set; }
    public string CreePar { get; set; } = string.Empty;
    public DateTime CreeLe { get; set; }
    public string? ModifiePar { get; set; }
    public DateTime? ModifieLe { get; set; }
    public string? NatureArticleCode { get; set; }
    public string? FamilleProduitFiniCode { get; set; }
    public string? PosteCode { get; set; }
    public string? PosteLibelle { get; set; }
    public string? Libre1 { get; set; }
    public string? Libre2 { get; set; }
    public string? Libre3 { get; set; }

    // For CTRL_POSTE documents: the JSON configuration of equipes/columns
    public string? ConfigurationColonnesJson { get; set; }
    // For CTRL_POSTE documents: the list of control lines (machine + defaut)
    public List<ControlePosteLigneDto> LignesControlePoste { get; set; } = new();

    public List<DocumentColonneDefDto> ColonneDefs { get; set; } = new();
    public List<DocumentSectionDto> Sections { get; set; } = new();
}

public class ControlePosteLigneDto
{
    public Guid Id { get; set; }
    public string? MachineCode { get; set; }
    public Guid? RisqueDefautId { get; set; }
    public string? LibelleDefaut { get; set; }
    public int OrdreAffiche { get; set; }
}

public class DocumentColonneDefDto
{
    public Guid Id { get; set; }
    public Guid EnteteId { get; set; }
    public string CleColonne { get; set; } = string.Empty;
    public string LabelAffiche { get; set; } = string.Empty;
    public string TypeValeur { get; set; } = "TEXTE";
    public int OrdreAffiche { get; set; }
    public string? InsertAfter { get; set; }
}

public class DocumentSectionDto
{
    public Guid Id { get; set; }
    public Guid EnteteId { get; set; }
    public int OrdreAffiche { get; set; }
    public string LibelleSection { get; set; } = string.Empty;
    public Guid? TypeSectionId { get; set; }
    public Guid? PeriodiciteId { get; set; }
    public Guid? RegleEchantillonnageId { get; set; }
    public string? Notes { get; set; }
    public string? NormeReference { get; set; }
    public int? NqaId { get; set; }

    public List<DocumentLigneDto> Lignes { get; set; } = new();
}

public class DocumentLigneDto
{
    public Guid Id { get; set; }
    public Guid EnteteId { get; set; }
    public Guid SectionId { get; set; }
    public int OrdreAffiche { get; set; }
    public Guid? CaracteristiqueId { get; set; }
    public string? LibelleAffiche { get; set; }
    public Guid? TypeCaracteristiqueId { get; set; }
    public string? TypeCaracteristiqueLibelle { get; set; }
    public Guid? TypeControleId { get; set; }
    public string? TypeControleLibelle { get; set; }
    public Guid? MoyenControleId { get; set; }
    public string? MoyenControleLibelle { get; set; }
    public string? MoyenTexteLibre { get; set; }
    public string? InstrumentCode { get; set; }
    public Guid? PeriodiciteId { get; set; }
    public string? PeriodiciteLibelle { get; set; }
    public string? LimiteSpecTexte { get; set; }
    public bool EstCritique { get; set; }
    public string? Instruction { get; set; }
    public string? Observations { get; set; }
    public string? ImageBase64 { get; set; }
    public string? MachineCode { get; set; }
    public bool EstVerifPresence { get; set; }
    public Guid? DefauthequeId { get; set; }
    public string? RefPlanProduit { get; set; }
    public string? MachineCodeCtrlPoste { get; set; }
    public Guid? RisqueDefautId { get; set; }
    
    public string? Libre1 { get; set; }
    public string? Libre2 { get; set; }
    public string? Libre3 { get; set; }
    public string? Libre4 { get; set; }
    public string? Libre5 { get; set; }

    public List<DocumentExtraColonneDto> ExtraColonnes { get; set; } = new();
}

public class DocumentExtraColonneDto
{
    public Guid Id { get; set; }
    public string CleColonne { get; set; } = string.Empty;
    public string? ValeurColonne { get; set; }
    public int OrdreAffiche { get; set; }
}

public class CreateDocumentRequestDto
{
    public string TypeDocumentCode { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public int? VersionInitiale { get; set; }
    public string? OperationCode { get; set; }
    public string? RefFormulaireCodeReference { get; set; }
    public string? LegendeMoyens { get; set; }
    public string? Remarques { get; set; }
    public string? NatureArticleCode { get; set; }
    public string? FamilleProduitFiniCode { get; set; }
    public string? PosteCode { get; set; }
    public string? Libre1 { get; set; }
    public string? Libre2 { get; set; }
    public string? Libre3 { get; set; }

    public string? ConfigurationColonnesJson { get; set; }

    public List<SopalTrace.Application.Helpers.ColonneJsonDto> ColonneDefs { get; set; } = new();

    public List<CreateDocumentSectionDto> Sections { get; set; } = new();
}

public class CreateDocumentSectionDto
{
    public int OrdreAffiche { get; set; }
    public string LibelleSection { get; set; } = string.Empty;
    public Guid? TypeSectionId { get; set; }
    public Guid? PeriodiciteId { get; set; }
    public Guid? RegleEchantillonnageId { get; set; }
    public string? Notes { get; set; }
    public string? NormeReference { get; set; }
    public int? NqaId { get; set; }

    public List<CreateDocumentLigneDto> Lignes { get; set; } = new();
}

public class CreateDocumentLigneDto
{
    public int OrdreAffiche { get; set; }
    public Guid? CaracteristiqueId { get; set; }
    public string? LibelleAffiche { get; set; }
    public Guid? TypeCaracteristiqueId { get; set; }
    public Guid? TypeControleId { get; set; }
    public Guid? MoyenControleId { get; set; }
    public string? MoyenTexteLibre { get; set; }
    public string? InstrumentCode { get; set; }
    public Guid? PeriodiciteId { get; set; }
    public string? LimiteSpecTexte { get; set; }
    public bool EstCritique { get; set; }
    public string? Instruction { get; set; }
    public string? Observations { get; set; }
    public string? ImageBase64 { get; set; }
    public string? MachineCode { get; set; }
    public bool EstVerifPresence { get; set; }
    public Guid? DefauthequeId { get; set; }
    public string? RefPlanProduit { get; set; }
    public string? MachineCodeCtrlPoste { get; set; }
    public Guid? RisqueDefautId { get; set; }

    public string? Libre1 { get; set; }
    public string? Libre2 { get; set; }
    public string? Libre3 { get; set; }
    public string? Libre4 { get; set; }
    public string? Libre5 { get; set; }

    public List<CreateDocumentExtraColonneDto> ExtraColonnes { get; set; } = new();
}

public class CreateDocumentExtraColonneDto
{
    public string CleColonne { get; set; } = string.Empty;
    public string? ValeurColonne { get; set; }
    public int OrdreAffiche { get; set; }
}

public class CreateDocumentColonneDefDto
{
    public string CleColonne { get; set; } = string.Empty;
    public string LabelAffiche { get; set; } = string.Empty;
    public string TypeValeur { get; set; } = "TEXTE";
    public int OrdreAffiche { get; set; }
}

public class NouvelleVersionDocumentRequestDto : CreateDocumentRequestDto
{
    public Guid AncienId { get; set; }
}

public class RestaurerDocumentRequestDto
{
    public Guid DocumentArchiveId { get; set; }
    public string MotifRestoration { get; set; } = string.Empty;
}

public class UpdateDocumentRequestDto
{
    public string? Nom { get; set; }
    public string? LegendeMoyens { get; set; }
    public string? Remarques { get; set; }
    public string? Libre1 { get; set; }
    
    public string? ConfigurationColonnesJson { get; set; }
    public string? RefFormulaireCodeReference { get; set; }
    
    public List<UpdateDocumentSectionDto> Sections { get; set; } = new();
}

public class UpdateDocumentSectionDto : CreateDocumentSectionDto
{
    public Guid? Id { get; set; }
    public new List<UpdateDocumentLigneDto> Lignes { get; set; } = new();
}

public class UpdateDocumentLigneDto : CreateDocumentLigneDto
{
    public Guid? Id { get; set; }
}

using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class DocumentEntete
{
    public Guid Id { get; set; }

    public string TypeDocumentCode { get; set; } = null!;

    public string Nom { get; set; } = null!;

    public string? Designation { get; set; }

    public int Version { get; set; }

    public string Statut { get; set; } = null!;

    public string? OperationCode { get; set; }

    public Guid? FormulaireId { get; set; }

    public string? LegendeMoyens { get; set; }

    public string? Remarques { get; set; }

    public string CreePar { get; set; } = null!;

    public DateTime CreeLe { get; set; }

    public string? ModifiePar { get; set; }

    public DateTime? ModifieLe { get; set; }

    public string? NatureArticleCode { get; set; }

    public string? FamilleProduitFiniCode { get; set; }

    public string? PosteCode { get; set; }

    public string? Libre1 { get; set; }

    public string? Libre2 { get; set; }

    public string? Libre3 { get; set; }

    public virtual ICollection<DocumentLigne> DocumentLignes { get; set; } = new List<DocumentLigne>();

    public virtual ICollection<DocumentSection> DocumentSections { get; set; } = new List<DocumentSection>();

    public virtual FamilleProduitFini? FamilleProduitFiniCodeNavigation { get; set; }

    public virtual RefFormulaire? Formulaire { get; set; }

    public virtual NatureArticle? NatureArticleCodeNavigation { get; set; }

    public virtual Operation? OperationCodeNavigation { get; set; }

    public virtual PosteTravail? PosteCodeNavigation { get; set; }

    public virtual TypeDocument TypeDocumentCodeNavigation { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class DocumentLigne
{
    public Guid Id { get; set; }

    public Guid EnteteId { get; set; }

    public Guid? SectionId { get; set; }

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

    public string? MachineCode { get; set; }

    public Guid? RisqueDefautId { get; set; }

    public string? TypeLigne { get; set; }

    public virtual RefCaracteristique? Caracteristique { get; set; }

    public virtual ICollection<DocumentLigneExtraColonne> DocumentLigneExtraColonnes { get; set; } = new List<DocumentLigneExtraColonne>();

    public virtual DocumentLineImage? DocumentLineImage { get; set; }

    public virtual ICollection<DocumentVerifMachineEcheance> DocumentVerifMachineEcheances { get; set; } = new List<DocumentVerifMachineEcheance>();

    public virtual DocumentEntete Entete { get; set; } = null!;

    public virtual ICollection<ExecControleLigneReponse> ExecControleLigneReponses { get; set; } = new List<ExecControleLigneReponse>();

    public virtual Instrument? InstrumentCodeNavigation { get; set; }

    public virtual Machine? MachineCodeNavigation { get; set; }

    public virtual MoyenControle? MoyenControle { get; set; }

    public virtual Periodicite? Periodicite { get; set; }

    public virtual RisqueDefaut? RisqueDefaut { get; set; }

    public virtual DocumentSection? Section { get; set; }

    public virtual TypeCaracteristique? TypeCaracteristique { get; set; }

    public virtual TypeControle? TypeControle { get; set; }
}

using System;
using System.Collections.Generic;

namespace SopalTrace.Domain.Entities;

public partial class DocumentVerifMachineEcheance
{
    public Guid Id { get; set; }

    public Guid PlanLigneId { get; set; }

    public Guid PeriodiciteMachineId { get; set; }

    public Guid? RefMoyenDetectionId { get; set; }

    public int OrdreAffiche { get; set; }

    public virtual ICollection<DocumentVerifMachineMatricePiece> DocumentVerifMachineMatricePieces { get; set; } = new List<DocumentVerifMachineMatricePiece>();

    public virtual ICollection<ExecVerifMachineReponse> ExecVerifMachineReponses { get; set; } = new List<ExecVerifMachineReponse>();

    public virtual PeriodiciteMachine PeriodiciteMachine { get; set; } = null!;

    public virtual DocumentVerifMachineLigne PlanLigne { get; set; } = null!;

    public virtual RefMoyenDetection? RefMoyenDetection { get; set; }
}

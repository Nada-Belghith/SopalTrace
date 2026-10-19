using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SopalTrace.Application.DTOs.Execution
{
    public class ExecRcPosteDto
    {
        public Guid ExecControleDocumentStatutId { get; set; }
        public Guid ExecControleOfId { get; set; }
        public string? PosteCode { get; set; }
        public string? Equipe { get; set; }
        
        // Plan data for display
        public string? PlanNom { get; set; }
        public string? PlanRemarques { get; set; }
        public string? PlanLegendeMoyens { get; set; }
        public string? ConfigurationColonnesJson { get; set; }

        public List<ExecRcPosteLigneDto> Lignes { get; set; } = new List<ExecRcPosteLigneDto>();
        public List<ExecRcPosteReponseDto> Reponses { get; set; } = new List<ExecRcPosteReponseDto>();
        public ExecRcPosteBilanDto Bilan { get; set; } = new();
        
        /// <summary>Jours de production du poste pour cet OF (le plus récent d'abord).</summary>
        public List<ExecRcPosteJourDto> Jours { get; set; } = new();

        /// <summary>Quantité lancée de l'OF : plafond des pièces conformes cumulées du poste.</summary>
        public int QuantiteLancee { get; set; }
        /// <summary>Pièces conformes cumulées du poste sur tous les documents (jours) de l'OF.</summary>
        public int CumulPiecesConformes { get; set; }
        /// <summary>Le cumul dépasse la quantité lancée (saisies à vérifier, à signaler au superviseur).</summary>
        public bool QuantiteLanceeDepassee { get; set; }

        /// <summary>Jour de production du document.</summary>
        public DateTime? JourProduction { get; set; }

        /// <summary>Équipes du document (définies par le superviseur) et leurs heures.</summary>
        public List<ExecRcPosteEquipeDto> Equipes { get; set; } = new();

        /// <summary>Le document ne définit pas d'équipes : les 3 équipes standard (6 h–14 h, 14 h–22 h, 22 h–6 h) s'appliquent.</summary>
        public bool EquipesParDefaut { get; set; }

        /// <summary>Équipe en production sur l'OF : la seule qui peut saisir, et seulement ses heures.</summary>
        public string? EquipeSaisie { get; set; }

        /// <summary>Tranches horaires de l'équipe qui saisit (calculées par le serveur).</summary>
        public List<string> TranchesEquipe { get; set; } = new();

        public bool PeutSaisir { get; set; }

        /// <summary>Pourquoi la saisie est fermée (document terminé, autre jour, aucune équipe en production…).</summary>
        public string? MotifLectureSeule { get; set; }

        /// <summary>
        /// Dernière saisie de l'équipe lue par l'écran ; renvoyée à l'enregistrement pour détecter une saisie faite
        /// entre-temps depuis une autre tablette.
        /// </summary>
        public DateTime? DerniereSaisieEquipe { get; set; }

        /// <summary>Pièces rebutées de l'équipe qui saisit (le total du document est dans le bilan).</summary>
        [Range(0, int.MaxValue)]
        public int? NbPiecesRebuteesEquipe { get; set; }

        public List<ExecRcPosteRebutDto> RebutsParEquipe { get; set; } = new();

        /// <summary>Équipes qui ont validé leurs heures sur ce document (lecture).</summary>
        public List<ExecRcPosteValidationDto> Validations { get; set; } = new();
    }

    public class ExecRcPosteEquipeDto
    {
        public string Code { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public int Debut { get; set; }
        public int Fin { get; set; }
        public List<string> Tranches { get; set; } = new();
    }

    public class ExecRcPosteJourDto
    {
        public Guid StatutId { get; set; }
        public DateTime? Jour { get; set; }
        public bool EstTermine { get; set; }
        public List<string> EquipesValidees { get; set; } = new();
    }

    public class ExecRcPosteRebutDto
    {
        public string? Equipe { get; set; }
        public int NbPiecesRebutees { get; set; }
    }

    public class ExecRcPosteValidationDto
    {
        public string Equipe { get; set; } = string.Empty;
        public DateTime? DateValidation { get; set; }
        public string? MatriculeOperateur { get; set; }
    }

    public class ExecRcPosteLigneDto
    {
        public Guid DocLigneId { get; set; }
        public string? MachineCode { get; set; }
        public Guid? RisqueDefautId { get; set; }
        public string? LibelleAffiche { get; set; }
        public int OrdreAffiche { get; set; }
        
        public List<ExecRcPosteHeureDto> Heures { get; set; } = new List<ExecRcPosteHeureDto>();
    }

    public class ExecRcPosteHeureDto
    {
        public Guid? Id { get; set; }
        public Guid DocLigneId { get; set; }
        public string TrancheHoraire { get; set; } = string.Empty;
        [Range(0, int.MaxValue)]
        public int NbNcParHeure { get; set; }
        public string? MatriculeOp { get; set; }
        public string? Equipe { get; set; }
    }

    public class ExecRcPosteReponseDto
    {
        public Guid? Id { get; set; }
        public string TrancheHoraire { get; set; } = string.Empty;
        /// <summary>Calculé par le serveur (somme des non-conformités de la tranche) ; ignoré en entrée.</summary>
        [Range(0, int.MaxValue)]
        public int TotalNcHeure { get; set; }
        [Range(0, int.MaxValue)]
        public int TotalRealiseHeure { get; set; }
        public string? Equipe { get; set; }
    }

    public class ExecRcPosteBilanDto
    {
        public Guid? Id { get; set; }
        [Range(0, int.MaxValue)]
        public int TotalDefauts { get; set; }
        [Range(0, int.MaxValue)]
        public int TotalPiecesTestees { get; set; }
        /// <summary>Calculé par le serveur ; ignoré en entrée.</summary>
        public double TauxNc { get; set; }
        [Range(0, int.MaxValue)]
        public int NbPiecesRebutees { get; set; }
        [Range(0, int.MaxValue)]
        public int NbPieceConforme { get; set; }
    }
}

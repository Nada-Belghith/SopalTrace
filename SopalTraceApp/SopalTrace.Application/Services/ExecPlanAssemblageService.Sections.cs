using SopalTrace.Application.DTOs.Execution;
using SopalTrace.Application.Helpers;
using SopalTrace.Application.Services.Execution;
using SopalTrace.Domain.Constants;
using SopalTrace.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace SopalTrace.Application.Services
{
    public partial class ExecPlanAssemblageService
    {
        public static string TypeDe(DocumentSection section) =>
            section.TypeSection?.TypeNormalise?.ToUpperInvariant() ?? "";

        /// <summary>Sections de lot du plan (LOT_POSTE, LOT_OF) avec l'état de leurs contrôles pour l'équipe active.</summary>
        public static List<ExecPlanAssemblageSectionDto> SectionsLot(DocumentEntete plan, ContexteEquipe equipe, IReadOnlyCollection<ReponseLot> reponses)
        {
            return plan.DocumentSections
                .Where(s => ControlesLot.EstSectionLot(TypeDe(s)))
                .OrderBy(s => s.OrdreAffiche)
                .Select(s =>
                {
                    var type = TypeDe(s);
                    var ligneIds = s.DocumentLignes.Select(l => l.Id).ToList();
                    return new ExecPlanAssemblageSectionDto
                    {
                        Id = s.Id,
                        Libelle = s.LibelleSection,
                        TypeSection = type,
                        Ordre = s.OrdreAffiche,
                        LignesPlan = s.DocumentLignes.OrderBy(l => l.OrdreAffiche).Select(MapLignePlan).ToList(),
                        PremierePiece = ControlesLot.Etat(ligneIds, ContextesReponse.PremierePiece,
                            ControlesLot.PremierePieceRequise(type, equipe), ControlesLot.PorteeLot(type, equipe), reponses),
                        DernierePiece = ControlesLot.Etat(ligneIds, ContextesReponse.DernierePiece,
                            ControlesLot.DernierePieceRequise(type, equipe), ControlesLot.PorteeLot(type, equipe), reponses)
                    };
                })
                .ToList();
        }

        /// <summary>Sections de réglage et « toutes les pièces » : saisies quand l'opérateur le décide.</summary>
        public static List<SectionALaDemandeDto> SectionsALaDemande(DocumentEntete plan, ContexteEquipe equipe, IReadOnlyCollection<ReponseLot> reponses)
        {
            return plan.DocumentSections
                .Select(s => (Section: s, Contexte: ControlesLot.ContexteALaDemande(TypeDe(s))))
                .Where(x => x.Contexte != null)
                .OrderBy(x => x.Section.OrdreAffiche)
                .Select(x =>
                {
                    var ligneIds = x.Section.DocumentLignes.Select(l => l.Id).ToList();
                    return new SectionALaDemandeDto
                    {
                        Id = x.Section.Id,
                        Libelle = x.Section.LibelleSection,
                        TypeSection = TypeDe(x.Section),
                        Ordre = x.Section.OrdreAffiche,
                        Contexte = x.Contexte!,
                        LignesPlan = x.Section.DocumentLignes.OrderBy(l => l.OrdreAffiche).Select(MapLignePlan).ToList(),
                        DernierControle = ControlesLot.Etat(ligneIds, x.Contexte!, requis: false, equipe.SessionId, reponses),
                        NombreControlesEquipe = reponses
                            .Where(r => r.Contexte == x.Contexte && r.SessionId == equipe.SessionId && ligneIds.Contains(r.LigneId))
                            .Select(r => r.DateSaisie)
                            .Distinct()
                            .Count()
                    };
                })
                .ToList();
        }

        private static ExecPlanAssemblageLignePlanDto MapLignePlan(DocumentLigne ligne)
        {
            return new ExecPlanAssemblageLignePlanDto
            {
                Id = ligne.Id,
                Numero = ligne.OrdreAffiche,
                Caracteristique = ligne.LibelleAffiche ?? ligne.Caracteristique?.Libelle ?? "",
                LimiteSpecTexte = ligne.LimiteSpecTexte,
                TypeControle = ligne.TypeControle?.Libelle ?? "",
                MoyenControle = !string.IsNullOrEmpty(ligne.MoyenTexteLibre) ? ligne.MoyenTexteLibre : ligne.MoyenControle?.Libelle ?? "",
                Instrument = ligne.InstrumentCode,
                Observations = ligne.Observations,
                ImageBase64 = ligne.EnDataUrl(),
                ExtraColonnes = ligne.DocumentLigneExtraColonnes
                    .Where(ec => !string.IsNullOrEmpty(ec.CleColonne))
                    .GroupBy(ec => ec.CleColonne)
                    .ToDictionary(g => g.Key, g => g.Last().ValeurColonne ?? "")
            };
        }
    }
}

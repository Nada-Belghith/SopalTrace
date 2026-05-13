using SopalTrace.Application.Interfaces;
using SopalTrace.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SopalTrace.Application.Utilities;

public static class SmartDictionaryHelper
{
    public static async Task ResolveAndCreateMissingReferencesAsync(
        IDictionnaireQualiteRepository repo,
        string? regleLibelle,
        Action<Guid?> setRegleId,
        IEnumerable<(string? libelleCarac, Action<Guid?> setCaracId, string? libelleMoyen, Action<Guid?> setMoyenId, string? codeInstrument)> lignes,
        HashSet<string> addedRegles,
        HashSet<string> addedCaracs,
        HashSet<string> addedMoyens,
        HashSet<string> addedInstruments)
    {
        // Regle Echantillonnage : on cherche uniquement en base, on ne crée JAMAIS une nouvelle règle
        // Si introuvable, le libellé est déjà concaténé dans LibelleSection par le mapper, l'ID reste null
        if (!string.IsNullOrWhiteSpace(regleLibelle))
        {
            var libelle = regleLibelle.Trim();
            var regle = await repo.GetRegleEchantillonnageByLibelleAsync(libelle);
            if (regle != null)
            {
                setRegleId(regle.Id);
            }
            // Si regle == null : on ne crée pas d'entrée, le texte est dans LibelleSection
        }

        foreach (var (libelleCarac, setCaracId, libelleMoyen, setMoyenId, codeInstrument) in lignes)
        {
            // Caractéristique
            if (!string.IsNullOrWhiteSpace(libelleCarac))
            {
                var typeCarac = await repo.GetTypeCaracteristiqueByLibelleAsync(libelleCarac);
                if (typeCarac == null && !addedCaracs.Contains(libelleCarac))
                {
                    typeCarac = new TypeCaracteristique
                    {
                        Id = Guid.NewGuid(),
                        Libelle = libelleCarac.Length > 80 ? libelleCarac.Substring(0, 80) : libelleCarac,
                        Code = $"CAR-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                        Actif = true
                    };
                    await repo.AddTypeCaracteristiqueAsync(typeCarac);
                    addedCaracs.Add(libelleCarac);
                    setCaracId(typeCarac.Id);
                }
                else if (typeCarac != null)
                {
                    setCaracId(typeCarac.Id);
                }
            }

            // Moyen de contrôle
            if (!string.IsNullOrWhiteSpace(libelleMoyen))
            {
                var moyen = await repo.GetMoyenControleByLibelleAsync(libelleMoyen);
                if (moyen == null && !addedMoyens.Contains(libelleMoyen))
                {
                    moyen = new MoyenControle
                    {
                        Id = Guid.NewGuid(),
                        Libelle = libelleMoyen.Length > 80 ? libelleMoyen.Substring(0, 80) : libelleMoyen,
                        Code = $"MC-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}",
                        Actif = true
                    };
                    await repo.AddMoyenControleAsync(moyen);
                    addedMoyens.Add(libelleMoyen);
                    setMoyenId(moyen.Id);
                }
                else if (moyen != null)
                {
                    setMoyenId(moyen.Id);
                }
            }

            // Instrument
            if (!string.IsNullOrWhiteSpace(codeInstrument))
            {
                var instrument = await repo.GetInstrumentByCodeAsync(codeInstrument);
                if (instrument == null && !addedInstruments.Contains(codeInstrument))
                {
                    instrument = new Instrument
                    {
                        CodeInstrument = codeInstrument.Length > 50 ? codeInstrument.Substring(0, 50) : codeInstrument,
                        Designation = codeInstrument,
                        Statut = "ACTIF",
                        Actif = true
                    };
                    await repo.AddInstrumentAsync(instrument);
                    addedInstruments.Add(codeInstrument);
                }
            }
        }
    }
}
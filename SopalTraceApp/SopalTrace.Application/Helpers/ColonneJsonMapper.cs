using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using SopalTrace.Domain.Entities;

namespace SopalTrace.Application.Helpers;

public class ColonneJsonDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? InsertAfter { get; set; }
    public string? TargetTable { get; set; }
}

public class EquipeJsonDto
{
    public string Nom { get; set; } = string.Empty;
    public int Debut { get; set; }
    public int Fin { get; set; }
}

public class FormulaireStructureRootDto
{
    public List<EquipeJsonDto> Equipes { get; set; } = new List<EquipeJsonDto>();
    public List<ColonneJsonDto> CustomCols { get; set; } = new List<ColonneJsonDto>();
}

public static class ColonneJsonMapper
{
    public static string? Serialize(IEnumerable<RefFormulaireColonneDef>? colonnes, IEnumerable<RefFormulaireEquipe>? equipes = null)
    {
        var root = new FormulaireStructureRootDto();

        if (equipes != null && equipes.Any())
        {
            root.Equipes = equipes.Where(e => e.Actif).OrderBy(e => e.OrdreAffiche).Select(e => new EquipeJsonDto
            {
                Nom = e.NomEquipe,
                Debut = e.HeureDebut,
                Fin = e.HeureFin
            }).ToList();
        }

        if (colonnes != null && colonnes.Any())
        {
            root.CustomCols = colonnes.Where(c => c.Actif).Select(c => new ColonneJsonDto
            {
                Key = c.CleColonne,
                Label = c.LabelAffiche,
                Type = c.TypeValeur,
                InsertAfter = c.InsertAfter,
                TargetTable = c.TargetTable
            }).ToList();
        }

        // Return old format (array) if no equipes, otherwise return the new object format
        if (!root.Equipes.Any())
        {
            return root.CustomCols.Any() ? JsonSerializer.Serialize(root.CustomCols, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) : null;
        }

        return JsonSerializer.Serialize(root, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    public static FormulaireStructureRootDto DeserializeRoot(string? json)
    {
        var root = new FormulaireStructureRootDto();
        if (string.IsNullOrWhiteSpace(json)) return root;

        try
        {
            // Essayer d'abord le nouveau format avec equipes et customCols
            var parsedRoot = JsonSerializer.Deserialize<FormulaireStructureRootDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (parsedRoot != null && (parsedRoot.Equipes.Any() || parsedRoot.CustomCols.Any()))
            {
                return parsedRoot;
            }
        }
        catch { /* Ignore */ }

        try
        {
            // Fallback: ancien format qui était un tableau direct de colonnes
            var parsedCols = JsonSerializer.Deserialize<List<ColonneJsonDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (parsedCols != null)
            {
                root.CustomCols = parsedCols;
            }
        }
        catch { /* Ignore */ }

        return root;
    }

    public static List<ColonneJsonDto> Deserialize(string? json)
    {
        return DeserializeRoot(json).CustomCols;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

public class Test
{
    public static void Main()
    {
        var mapFamilles = new Dictionary<int, string>
        {
            { 6, "Famille Corps (30, 35)" },
            { 7, "Famille Corps (23)" },
            { 8, "Famille Corps (40, 43, 44)" },
            { 9, "Famille Corps (49)" }
        };

        var resultFamilles = mapFamilles.Values.Distinct().ToList();
        
        Console.WriteLine("API Returns Familles:");
        foreach (var fam in resultFamilles)
        {
            Console.WriteLine(fam);
        }
    }
}

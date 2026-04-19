namespace SopalTrace.Application.Utilities;

public static class CodeGenerator
{
    /// <summary>
    /// Generates a code from a libelle by converting to uppercase and replacing spaces/special chars with underscores.
    /// Example: "RAYON COURBURE" -> "RAYON_COURBURE"
    /// </summary>
    public static string GenerateCodeFromLibelle(string libelle, int maxLength = 30)
    {
        if (string.IsNullOrWhiteSpace(libelle))
            return string.Empty;

        var code = libelle.ToUpper().Replace(" ", "_").Replace("'", "_");
        
        if (code.Length > maxLength)
            code = code.Substring(0, maxLength);

        return code;
    }
}

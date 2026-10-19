using System.Security.Cryptography;
using System.Text;

namespace SopalTrace.Application.Helpers;

/// <summary>Jetons envoyés par e-mail ou en cookie : seule leur empreinte est conservée en base.</summary>
public static class JetonsSecurite
{
    /// <summary>256 bits aléatoires, encodés pour une URL (base64url sans remplissage).</summary>
    public static string Generer() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Empreinte SHA-256 en hexadécimal (64 caractères) : une fuite de la base ne permet pas de rejouer le jeton.</summary>
    public static string Empreinte(string jeton) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(jeton)));
}

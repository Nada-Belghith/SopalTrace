namespace SopalTrace.Domain.Constants;

/// <summary>Rôle d'une pièce de référence dans une vérification machine (ReferencePieceRole.Code).</summary>
public static class RolesPieceReference
{
    public const string Conforme = "PRC";
    public const string NonConforme = "PRNC";
    public const string FuiteEtalonConforme = "FEC";
    public const string FuiteEtalonNonConforme = "FENC";

    /// <summary>Pièce de fuite étalon : le point est un contrôle d'étanchéité (pression, ΔP).</summary>
    public static bool EstFuiteEtalon(string? role) => role is FuiteEtalonConforme or FuiteEtalonNonConforme;
}

/// <summary>Types de robinet (TypeRobinet.Code).</summary>
public static class TypesRobinet
{
    public const string Manuel = "MAN";
    public const string Automatique = "AUTO";
    public const string AvecSoupape = "SOUP";
}

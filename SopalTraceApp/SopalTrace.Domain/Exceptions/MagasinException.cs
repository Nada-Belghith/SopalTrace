namespace SopalTrace.Domain.Exceptions;

/// <summary>Étiquette déjà scannée (même lot pour le même article, ou OF déjà associé) : 409, la saisie est ignorée.</summary>
public class ScanEnDoubleException : DomainException
{
    public ScanEnDoubleException(string message) : base(message) { }
}

/// <summary>Contenu lu par la douchette illisible ou au mauvais format : 400.</summary>
public class EtiquetteIllisibleException : DomainException
{
    public EtiquetteIllisibleException(string message) : base(message) { }
}

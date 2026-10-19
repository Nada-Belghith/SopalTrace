namespace SopalTrace.Domain.Exceptions;

/// <summary>Le service de conversion PDF est arrêté ou injoignable (HTTP 503) : la génération reprendra plus tard.</summary>
public class ServiceConversionIndisponibleException : DomainException
{
    public ServiceConversionIndisponibleException(string message) : base(message) { }
}

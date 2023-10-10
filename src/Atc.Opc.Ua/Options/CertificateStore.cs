namespace Atc.Opc.Ua.Options;

/// <summary>
/// Certificate store
/// </summary>
public class CertificateStore
{
    /// <summary>
    /// Store type
    /// </summary>
    public string? StoreType { get; set; }

    /// <summary>
    /// Store path
    /// </summary>
    public string? StorePath { get; set; }
}
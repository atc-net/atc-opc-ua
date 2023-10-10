namespace Atc.Opc.Ua.Options;

/// <summary>
/// Certificate information
/// </summary>
public class CertificateInfo : CertificateStore
{
    /// <summary>
    /// Subject name
    /// </summary>
    public string? SubjectName { get; set; }
}
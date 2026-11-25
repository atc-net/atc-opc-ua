namespace Atc.Opc.Ua.Options;

/// <summary>
/// Certificate information
/// </summary>
public class CertificateInfo : CertificateStore
{
    /// <summary>
    /// The certificate's subject, e.g. <c>CN=MyClient</c>. A plain name, e.g. <c>MyClient</c>, becomes
    /// <c>CN=MyClient</c>. Without one, the application name is used.
    /// </summary>
    public string? SubjectName { get; set; }
}
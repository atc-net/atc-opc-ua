namespace Atc.Opc.Ua.Services;

[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK")]
public partial class OpcUaScanner
{
    private readonly ILogger<OpcUaScanner> logger;

    [LoggerMessage(
        EventId = LoggingEventIdConstants.ScannerStart,
        Level = LogLevel.Information,
        Message = "Scanning starting nodeId '{NodeId}' with objectDepth={ObjectDepth}, variableDepth={VariableDepth}, includeSampleValues={IncludeSampleValues}.")]
    private partial void LogScannerStart(
        string nodeId,
        int objectDepth,
        int variableDepth,
        bool includeSampleValues);
}
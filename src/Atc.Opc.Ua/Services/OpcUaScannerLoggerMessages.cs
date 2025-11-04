namespace Atc.Opc.Ua.Services;

/// <summary>
/// OpcUaScanner LoggerMessages.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - By Design")]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK")]
public partial class OpcUaScanner
{
    private readonly ILogger<OpcUaScanner> logger;

    [LoggerMessage(
        EventId = LoggingEventIdConstants.ScannerClientNotConnected,
        Level = LogLevel.Warning,
        Message = "Scanner invoked while client not connected.")]
    private partial void LogScannerClientNotConnected();

    [LoggerMessage(
        EventId = LoggingEventIdConstants.ScannerStart,
        Level = LogLevel.Information,
        Message = "Scanning starting nodeId '{NodeId}' with objectDepth={ObjectDepth}, variableDepth={VariableDepth}, includeSampleValues={IncludeSampleValues}.")]
    private partial void LogScannerStart(
        string nodeId,
        int objectDepth,
        int variableDepth,
        bool includeSampleValues);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.ScannerReadRootFailure,
        Level = LogLevel.Error,
        Message = "Failed reading starting node '{NodeId}': '{Error}'")]
    private partial void LogScannerReadRootFailure(
        string nodeId,
        string error);
}
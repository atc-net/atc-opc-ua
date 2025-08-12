namespace Atc.Opc.Ua.Services;

/// <summary>
/// OpcUaScanner LoggerMessages.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - By Design")]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK")]
public partial class OpcUaScanner
{
    [LoggerMessage(
        EventId = LoggingEventIdConstants.ScannerClientNotConnected,
        Level = LogLevel.Warning,
        Message = "Scanner invoked while client not connected.")]
    private partial void LogScannerClientNotConnected();

    [LoggerMessage(
        EventId = LoggingEventIdConstants.ScannerStart,
        Level = LogLevel.Information,
        Message = "Scanning starting nodeId '{nodeId}' with objectDepth={objectDepth}, variableDepth={variableDepth}, includeSampleValues={includeSampleValues}.")]
    private partial void LogScannerStart(
        string nodeId,
        int objectDepth,
        int variableDepth,
        bool includeSampleValues);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.ScannerReadRootFailure,
        Level = LogLevel.Error,
        Message = "Failed reading starting node '{nodeId}': '{error}'.")]
    private partial void LogScannerReadRootFailure(
        string nodeId,
        string error);
}
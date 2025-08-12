namespace Atc.Opc.Ua.Services;

public interface IOpcUaScanner
{
    Task<NodeScanResult> ScanAsync(
        IOpcUaClient client,
        OpcUaScannerOptions options);
}
namespace Atc.Opc.Ua.Protocol.Runtime;

/// <summary>
/// The telemetry context the OPC UA stack logs, measures and traces through.
/// </summary>
internal sealed class StackTelemetryContext : TelemetryContextBase
{
    public StackTelemetryContext(ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
    }
}
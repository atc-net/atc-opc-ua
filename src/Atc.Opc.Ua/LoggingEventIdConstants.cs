namespace Atc.Opc.Ua;

internal static class LoggingEventIdConstants
{
    public const int SessionConnecting = 10000;
    public const int SessionConnected = 10001;
    public const int SessionConnectionFailure = 10002;

    public const int SessionAlreadyConnected = 10010;
    public const int SessionNotConnected = 10011;
    public const int SessionUntrustedCertificateAccepted = 10012;

    public const int SessionDisconnecting = 10020;
    public const int SessionDisconnected = 10021;
    public const int SessionDisconnectionFailure = 10022;

    public const int SessionNodeNotFound = 10030;
    public const int SessionParentNodeNotFound = 10031;
    public const int SessionLoadComplexTypeSystem = 10032;
    public const int SessionNodeHasWrongClass = 10033;

    public const int SessionReadNodeObject = 10040;
    public const int SessionReadNodeObjectWithMaxDepth = 10041;
    public const int SessionReadNodeObjectSucceeded = 10042;
    public const int SessionReadNodeVariableSucceeded = 10043;
    public const int SessionReadNodeVariableValueFailure = 10044;
    public const int SessionReadNodeNotSupportedNodeClass = 10045;
    public const int SessionReadNodeFailure = 10046;
    public const int SessionReadParentNodeFailure = 10047;

    public const int SessionWriteNodeVariableFailure = 10050;

    public const int SessionExecuteCommandRequest = 10060;
    public const int SessionExecuteCommandFailure = 10061;
}
namespace Atc.Opc.Ua.Services;

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
    public const int SessionReadNodeObject = 10033;
    public const int SessionReadNodeObjectWithMaxDepth = 10034;
    public const int SessionReadNodeObjectSucceeded = 10035;
    public const int SessionReadNodeVariableSucceeded = 10036;
    public const int SessionReadNodeVariableValueFailure = 10037;
    public const int SessionWriteNodeVariableFailure = 10038;
    public const int SessionNodeHasWrongClass = 10039;
    public const int SessionReadNodeNotSupportedNodeClass = 10040;
    public const int SessionReadNodeFailure = 10041;
}
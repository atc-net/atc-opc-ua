namespace Atc.Opc.Ua;

internal static class LoggingEventIdConstants
{
    // OpcUaClient
    public const int SessionConnecting = 10000;
    public const int SessionConnected = 10001;
    public const int SessionConnectionFailure = 10002;
    public const int SessionReconnected = 10003;
    public const int SessionReconnectFailure = 10004;

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
    public const int SessionReadNodeVariable = 10043;
    public const int SessionReadNodeVariableSucceeded = 10044;
    public const int SessionReadNodeVariableValueEmpty = 10045;
    public const int SessionReadNodeNotSupportedNodeClass = 10046;
    public const int SessionReadNodeFailure = 10047;
    public const int SessionReadParentNodeFailure = 10048;
    public const int SessionHandlingNode = 10049;

    public const int SessionWriteNodeVariableFailure = 10050;

    public const int SessionExecuteCommandRequest = 10060;
    public const int SessionExecuteCommandFailure = 10061;

    public const int SessionKeepAliveRequestFailure = 10070;
    public const int SessionKeepAliveFailureCountReset = 10071;
    public const int SessionKeepAliveFailure = 10072;

    public const int SessionReadEnumDataType = 10080;
    public const int SessionReadEnumDataTypeSucceeded = 10081;
    public const int SessionReadEnumDataTypeFailure = 10082;
    public const int SessionReadEnumDataTypeNotEnum = 10083;
    public const int SessionReadDataTypeDefinitionFailed = 10084;
    public const int SessionReadEnumValuesFailed = 10085;
    public const int SessionReadEnumStringsFailed = 10086;
    public const int SessionBrowsePropertyFailed = 10087;

    // OpcUaScanner
    public const int ScannerClientNotConnected = 11000;
    public const int ScannerStart = 11001;
    public const int ScannerReadRootFailure = 11002;
}
namespace Atc.Opc.Ua;

internal static class LoggingEventIdConstants
{
    // OpcUaClient
    public const int SessionConnecting = 10000;
    public const int SessionConnected = 10001;
    public const int SessionReconnected = 10003;
    public const int SessionReconnectFailure = 10004;
    public const int SessionReconnecting = 10005;
    public const int SessionReplacedAfterReconnect = 10006;

    public const int SessionAlreadyConnected = 10010;
    public const int ServerCertificateUntrustedAccepted = 10012;
    public const int ServerCertificateAcceptedWithoutValidation = 10013;
    public const int ServerCertificateValidationWouldFail = 10014;
    public const int ServerCertificateRejected = 10015;

    public const int SessionDisconnecting = 10020;
    public const int SessionDisconnected = 10021;
    public const int SessionClosingStale = 10023;
    public const int SessionCloseStaleFailure = 10024;

    public const int SessionNodeNotFound = 10030;
    public const int SessionParentNodeNotFound = 10031;
    public const int SessionLoadComplexTypeSystem = 10032;
    public const int SessionNodeLeftOut = 10034;

    public const int SessionReadNodeObject = 10040;
    public const int SessionReadNodeObjectWithMaxDepth = 10041;
    public const int SessionReadNodeObjectSucceeded = 10042;
    public const int SessionReadNodeVariable = 10043;
    public const int SessionReadNodeVariableSucceeded = 10044;
    public const int SessionReadNodeVariableValueEmpty = 10045;
    public const int SessionReadNodeNotSupportedNodeClass = 10046;
    public const int SessionNodeSkipped = 10047;
    public const int SessionReadParentNodeFailure = 10048;
    public const int SessionHandlingNode = 10049;
    public const int SessionReadNodeVariableValueNull = 10051;

    public const int SessionExecuteCommandRequest = 10060;

    public const int SessionKeepAliveRequestFailure = 10070;
    public const int SessionKeepAliveFailureCountReset = 10071;
    public const int SessionKeepAliveFailure = 10072;

    public const int SessionReadEnumDataType = 10080;
    public const int SessionReadEnumDataTypeSucceeded = 10081;
    public const int SessionReadDataTypeDefinitionFailed = 10084;
    public const int SessionReadEnumValuesFailed = 10085;
    public const int SessionReadEnumStringsFailed = 10086;
    public const int SessionBrowsePropertyFailed = 10087;

    // OpcUaClient - Subscription
    public const int SubscriptionCreating = 10090;
    public const int SubscriptionCreated = 10091;
    public const int SubscriptionRemoving = 10093;
    public const int SubscriptionRemoved = 10094;
    public const int SubscriptionNodeSubscribing = 10096;
    public const int SubscriptionNodeSubscribed = 10097;
    public const int SubscriptionNodeRejected = 10098;
    public const int SubscriptionNodeUnsubscribing = 10099;
    public const int SubscriptionNodeUnsubscribed = 10100;
    public const int SubscriptionUnsubscribeAll = 10102;
    public const int SubscriptionUnsubscribedAll = 10103;
    public const int SubscriptionNotificationError = 10104;
    public const int SubscriptionRestoredAfterReconnect = 10106;
    public const int SubscriptionLostAfterReconnect = 10107;
    public const int SubscriptionNodesSubscribed = 10108;

    // OpcUaNodeBrowser
    public const int BrowseChildren = 10110;
    public const int BrowseChildrenSucceeded = 10111;
    public const int ReadAttributes = 10113;
    public const int ReadAttributesSucceeded = 10114;

    // OpcUaScanner
    public const int ScannerStart = 11001;
}
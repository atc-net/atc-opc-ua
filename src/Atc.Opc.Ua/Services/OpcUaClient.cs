// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
// ReSharper disable InvertIf
namespace Atc.Opc.Ua.Services;

/// <summary>
/// OpcUaClient.
/// </summary>
[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OK - By Design")]
public partial class OpcUaClient : IOpcUaClient
{
    private const uint SessionTimeout = 30 * 60 * 1000;
    private const string ApplicationName = nameof(OpcUaClient);
    private readonly ApplicationConfiguration configuration;

    private readonly List<NodeId> excludeNodes = new()
    {
        ObjectIds.Server,
    };

    public Session? Session { get; private set; }

    public OpcUaClient(
        ILogger<OpcUaClient> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var application = BuildAndValidateOpcUaApplicationAsync().GetAwaiter().GetResult();
        this.configuration = application.ApplicationConfiguration;
        this.configuration.CertificateValidator.CertificateValidation += CertificateValidation;
    }

    public Task<bool> ConnectAsync(
        Uri serverUri)
        => ConnectAsync(serverUri, string.Empty, string.Empty);

    public Task<bool> ConnectAsync(
        Uri serverUri,
        string userName,
        string password)
    {
        ArgumentNullException.ThrowIfNull(serverUri);

        return InvokeConnectAsync(serverUri, userName, password);
    }

    private async Task<bool> InvokeConnectAsync(
        Uri serverUri,
        string userName,
        string password)
    {
        try
        {
            if (IsConnected())
            {
                LogSessionAlreadyConnected();
            }
            else
            {
                LogSessionConnecting();

                // Get the endpoint by connecting to server's discovery endpoint.
                // Try to find the first endpoint without security.
                var endpointDescription = CoreClientUtils.SelectEndpoint(serverUri.AbsoluteUri, useSecurity: false);

                var endpointConfiguration = EndpointConfiguration.Create(configuration);
                var endpoint = new ConfiguredEndpoint(collection: null, endpointDescription, endpointConfiguration);

                var userIdentity = BuildUserIdentity(userName, password);
                var session = await CreateSession(endpoint, userIdentity);

                if (session.Connected)
                {
                    Session = session;
                }

                LogSessionConnected(Session?.SessionName ?? "unknown");
            }

            return true;
        }
        catch (Exception ex)
        {
            LogSessionConnectionFailure(ex.Message);
            return false;
        }
    }

    public bool IsConnected()
        => Session is not null && Session.Connected;

    public bool Disconnect()
    {
        try
        {
            if (Session != null)
            {
                var sessionName = Session.SessionName ?? "unknown";
                LogSessionDisconnecting(sessionName);

                Session.Close();
                Session.Dispose();
                Session = null;

                LogSessionDisconnected(sessionName);
                return true;
            }

            LogSessionNotConnected();
            return false;
        }
        catch (Exception ex)
        {
            LogSessionDisconnectionFailure(ex.Message);
            return false;
        }
    }

    public Task<NodeVariable?> ReadNodeVariableAsync(
        string nodeId)
    {
        ArgumentNullException.ThrowIfNull(nodeId);

        return InvokeReadNodeVariableAsync(nodeId);
    }

    public Task<IList<NodeVariable>?> ReadNodeVariablesAsync(
        string[] nodeIds)
    {
        ArgumentNullException.ThrowIfNull(nodeIds);

        return InvokeReadNodeVariablesAsync(nodeIds);
    }

    public Task<NodeObject?> ReadNodeObjectAsync(
        string nodeId,
        bool includeObjects,
        bool includeVariables,
        int nodeObjectReadDepth = 1)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            return Task.FromResult((NodeObject?)null);
        }

        if (!IsConnected())
        {
            LogSessionNotConnected();
            return Task.FromResult((NodeObject?)null);
        }

        LogSessionReadNodeObjectWithMaxDepth(nodeId, nodeObjectReadDepth);

        var readNode = Session!.ReadNode(nodeId);
        if (readNode is null)
        {
            LogSessionNodeNotFound(nodeId);
            return Task.FromResult((NodeObject?)null);
        }

        if (readNode.NodeClass != NodeClass.Object)
        {
            LogSessionNodeHasWrongClass(nodeId, readNode.NodeClass, NodeClass.Object);
            return Task.FromResult((NodeObject?)null);
        }

        var parentNodeId = GetParentNodeId(nodeId);
        if (parentNodeId is null)
        {
            LogSessionParentNodeNotFound(nodeId);
            return Task.FromResult((NodeObject?)null);
        }

        var nodeObject = readNode.MapToNodeObject(parentNodeId);

        if (nodeObject is not null &&
            (includeObjects || includeVariables) &&
            nodeObjectReadDepth >= 1)
        {
            ReadChildNodes(nodeObject, 1, includeObjects, includeVariables, nodeObjectReadDepth);
        }

        LogSessionReadNodeObjectSucceeded(nodeId);
        return Task.FromResult(nodeObject);
    }

    public bool WriteNode(
        string nodeId,
        object value)
    {
        var nodesToWriteCollection = new WriteValueCollection();
        var writeVal = BuildWriteValue(nodeId, value);
        nodesToWriteCollection.Add(writeVal);

        return WriteNodeValueCollection(nodesToWriteCollection);
    }

    public bool WriteNodes(
        IDictionary<string, object> nodesToWrite)
    {
        ArgumentNullException.ThrowIfNull(nodesToWrite);

        var nodesToWriteCollection = new WriteValueCollection();
        foreach (var (nodeId, value) in nodesToWrite)
        {
            var writeVal = BuildWriteValue(nodeId, value);
            nodesToWriteCollection.Add(writeVal);
        }

        return WriteNodeValueCollection(nodesToWriteCollection);
    }

    private bool WriteNodeValueCollection(
        WriteValueCollection nodesToWriteCollection)
    {
        try
        {
            Session!.Write(
                requestHeader: null,
                nodesToWriteCollection,
                out StatusCodeCollection results,
                out DiagnosticInfoCollection _);

            if (results is not null &&
                results.Any() &&
                StatusCode.IsGood(results[0]))
            {
                return true;
            }

            LogSessionWriteNodeVariableFailure(results![0].ToString());
            return false;
        }
        catch (Exception ex)
        {
            LogSessionWriteNodeVariableFailure(ex.Message);
            return false;
        }
    }

    private static WriteValue BuildWriteValue(
        string nodeId,
        object value)
        => new()
        {
            NodeId = new NodeId(nodeId),
            AttributeId = Attributes.Value,
            Value = new DataValue
            {
                Value = value,
            },
        };

    private string? GetParentNodeId(
        string nodeId)
    {
        if (nodeId.Equals(ObjectIds.RootFolder.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var browseParentResults = BrowseBackwardsByNodeId(nodeId);
        if (browseParentResults.Count != 1)
        {
            return null;
        }

        var parentObject = browseParentResults[0];
        return parentObject.NodeId.ToString();
    }

    private void ReadChildNodes(
        NodeObject currentNode,
        int level,
        bool includeObjects,
        bool includeVariables,
        int nodeObjectReadDepth)
    {
        ArgumentNullException.ThrowIfNull(currentNode);

        if (excludeNodes.Find(x => x.ToString().Equals(currentNode.NodeId, StringComparison.Ordinal)) != null)
        {
            return;
        }

        LogSessionReadNodeObject(currentNode.NodeId);

        var browseChildResults = BrowseForwardByNodeId(currentNode.NodeId);
        if (!browseChildResults.Any())
        {
            return;
        }

        foreach (var result in browseChildResults)
        {
            HandleChildBrowseResults(currentNode, level, includeObjects, includeVariables, nodeObjectReadDepth, result);
        }
    }

    private Task<NodeVariable?> InvokeReadNodeVariableAsync(
        string nodeId)
    {
        if (!IsConnected())
        {
            LogSessionNotConnected();
            return Task.FromResult((NodeVariable?)null);
        }

        try
        {
            LogSessionReadNodeObject(nodeId);

            var readNode = Session!.ReadNode(nodeId);
            if (readNode is null)
            {
                LogSessionNodeNotFound(nodeId);
                return Task.FromResult((NodeVariable?)null);
            }

            if (readNode.NodeClass != NodeClass.Variable)
            {
                LogSessionNodeHasWrongClass(nodeId, readNode.NodeClass, NodeClass.Variable);
                return Task.FromResult((NodeVariable?)null);
            }

            var browserParent = BrowserFactory.GetBackwardsBrowser(Session!);
            var browseParentResults = browserParent.Browse(new NodeId(nodeId));
            if (browseParentResults is null || browseParentResults.Count != 1)
            {
                LogSessionParentNodeNotFound(nodeId);
                return Task.FromResult((NodeVariable?)null);
            }

            var sampleValue = TryGetDataValueForVariable(nodeId);
            var parentObject = browseParentResults[0];
            var nodeVariable = readNode.MapToNodeVariableWithValue(parentObject.NodeId.ToString(), sampleValue);

            LogSessionReadNodeVariableSucceeded(nodeId);
            return Task.FromResult(nodeVariable);
        }
        catch (Exception ex)
        {
            LogSessionReadNodeFailure(nodeId, ex.Message);
            return Task.FromResult((NodeVariable?)null);
        }
    }

    private async Task<IList<NodeVariable>?> InvokeReadNodeVariablesAsync(
        string[] nodeIds)
    {
        if (!nodeIds.Any())
        {
            return null;
        }

        if (!IsConnected())
        {
            LogSessionNotConnected();
            return null;
        }

        var result = new List<NodeVariable>();
        foreach (var nodeId in nodeIds)
        {
            var nodeVariable = await ReadNodeVariableAsync(nodeId);
            if (nodeVariable is not null)
            {
                result.Add(nodeVariable);
            }
        }

        return result;
    }

    private void HandleChildBrowseResults(
        NodeObject currentNode,
        int level,
        bool includeObjects,
        bool includeVariables,
        int nodeObjectReadDepth,
        ReferenceDescription result)
    {
        switch (result.NodeClass)
        {
            case NodeClass.Object:
                if (includeObjects)
                {
                    var childNode = result.MapToNodeObject(currentNode.NodeId);
                    if (childNode is not null)
                    {
                        if (nodeObjectReadDepth > level)
                        {
                            ReadChildNodes(childNode, level + 1, includeObjects, includeVariables, nodeObjectReadDepth);
                        }

                        currentNode.NodeObjects.Add(childNode);
                    }
                }

                break;
            case NodeClass.Variable:
                if (includeVariables)
                {
                    HandleBrowseResultVariableNode(currentNode, result);
                }

                break;
            default:
                LogSessionReadNodeNotSupportedNodeClass(currentNode.NodeId, result.NodeClass);
                return;
        }
    }

    private ReferenceDescriptionCollection BrowseForwardByNodeId(
        string nodeId)
    {
        try
        {
            var browser = BrowserFactory.GetForwardBrowser(Session!);
            return browser.Browse(new NodeId(nodeId));
        }
        catch (Exception ex)
        {
            LogSessionReadNodeFailure(nodeId, ex.Message);
            return new ReferenceDescriptionCollection();
        }
    }

    private ReferenceDescriptionCollection BrowseBackwardsByNodeId(
        string nodeId)
    {
        try
        {
            var browser = BrowserFactory.GetBackwardsBrowser(Session!);
            return browser.Browse(new NodeId(nodeId));
        }
        catch (Exception ex)
        {
            LogSessionReadNodeFailure(nodeId, ex.Message);
            return new ReferenceDescriptionCollection();
        }
    }

    private void HandleBrowseResultVariableNode(
        NodeObject node,
        ReferenceDescription referenceDescription)
    {
        var childNodeId = referenceDescription.NodeId.ToString();
        var childNode = Session!.ReadNode(childNodeId);
        var sampleValue = TryGetDataValueForVariable(childNodeId);
        var nodeVariable = childNode.MapToNodeVariableWithValue(node.NodeId, sampleValue);
        if (nodeVariable is not null)
        {
            node.NodeVariables.Add(nodeVariable);
        }
    }

    private DataValue? TryGetDataValueForVariable(
        string nodeId)
    {
        DataValue? sampleValue = null;

        var nodesToRead = new ReadValueIdCollection
        {
            new()
            {
                NodeId = nodeId,
                AttributeId = Attributes.Value,
            },
        };

        Session!.Read(
            requestHeader: null,
            0,
            TimestampsToReturn.Both,
            nodesToRead,
            out DataValueCollection resultValues,
            out DiagnosticInfoCollection _);

        if (resultValues is not null &&
            resultValues.Any() &&
            resultValues.Count == 1)
        {
            if (resultValues[0].Value is null)
            {
                LogSessionReadNodeVariableValueFailure(nodeId);
                return sampleValue;
            }

            sampleValue = resultValues[0];
        }

        return sampleValue;
    }

    private void CertificateValidation(
        CertificateValidator sender,
        CertificateValidationEventArgs e)
    {
        const bool certificateAccepted = true;

        //// ****
        //// Implement a custom logic to decide if the certificate should be accepted or not and set certificateAccepted flag accordingly.
        //// The certificate can be retrieved from the e.Certificate field
        //// ***

        if (certificateAccepted)
        {
            LogSessionUntrustedCertificateAccepted(e.Certificate.SubjectName.Name);
        }

        e.Accept = certificateAccepted;
    }

    private Task<Session> CreateSession(
        ConfiguredEndpoint endpoint,
        IUserIdentity userIdentity)
        => Session.Create(
            configuration,
            endpoint,
            updateBeforeConnect: false,
            checkDomain: false,
            configuration.ApplicationName + Guid.NewGuid(),
            SessionTimeout,
            userIdentity,
            preferredLocales: null);

    private static UserIdentity BuildUserIdentity(
        string userName,
        string password)
    {
        UserIdentity userIdentity;

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            userIdentity = new UserIdentity();
        }
        else
        {
            var userNameIdentityToken = new UserNameIdentityToken
            {
                UserName = userName,
                DecryptedPassword = password,
            };

            userIdentity = new UserIdentity(userNameIdentityToken);
        }

        return userIdentity;
    }

    private static async Task<ApplicationInstance> BuildAndValidateOpcUaApplicationAsync()
    {
        var applicationConfiguration = ApplicationConfigurationFactory.Create(ApplicationName);
        applicationConfiguration = ApplicationInstance.FixupAppConfig(applicationConfiguration);
        await applicationConfiguration.Validate(applicationConfiguration.ApplicationType);

        var application = new ApplicationInstance(applicationConfiguration);

        var hasAppCertificate =
            await application.CheckApplicationInstanceCertificate(
                silent: true,
                CertificateFactory.DefaultKeySize,
                CertificateFactory.DefaultHashSize);

        if (!hasAppCertificate)
        {
            throw new CertificateValidationException("OPC UA application certificate can not be validated");
        }

        return application;
    }
}
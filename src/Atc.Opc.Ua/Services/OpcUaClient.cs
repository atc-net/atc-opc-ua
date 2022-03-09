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
                var endpointDescription = CoreClientUtils.SelectEndpoint(serverUri.AbsoluteUri, useSecurity: true);

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

    public async Task<NodeObject?> ReadNodeObjectAsync(
        string nodeId,
        bool includeObjects,
        bool includeVariables,
        int nodeObjectReadDepth = 1)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            return null;
        }

        if (!IsConnected())
        {
            LogSessionNotConnected();
            return null;
        }

        LogSessionReadNodeObjectWithMaxDepth(nodeId, nodeObjectReadDepth);

        var readNode = Session!.ReadNode(nodeId);
        if (readNode is null)
        {
            LogSessionNodeNotFound(nodeId);
            return null;
        }

        if (readNode.NodeClass != NodeClass.Object)
        {
            LogSessionNodeHasWrongClass(nodeId, readNode.NodeClass, NodeClass.Object);
            return null;
        }

        var parentNodeId = GetParentNodeId(nodeId);
        if (parentNodeId is null)
        {
            LogSessionParentNodeNotFound(nodeId);
            return null;
        }

        var nodeObject = readNode.MapToNodeObject(parentNodeId);

        if (nodeObject is not null &&
            (includeObjects || includeVariables) &&
            nodeObjectReadDepth >= 1)
        {
            await ReadChildNodes(nodeObject, 1, includeObjects, includeVariables, nodeObjectReadDepth);
        }

        LogSessionReadNodeObjectSucceeded(nodeId);
        return nodeObject;
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

    private async Task ReadChildNodes(
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
            await HandleChildBrowseResults(currentNode, level, includeObjects, includeVariables, nodeObjectReadDepth, result);
        }
    }

    private async Task<NodeVariable?> InvokeReadNodeVariableAsync(
        string nodeId)
    {
        if (!IsConnected())
        {
            LogSessionNotConnected();
            return null;
        }

        try
        {
            LogSessionReadNodeObject(nodeId);

            var readNode = Session!.ReadNode(nodeId);
            if (readNode is null)
            {
                LogSessionNodeNotFound(nodeId);
                return null;
            }

            if (readNode.NodeClass != NodeClass.Variable)
            {
                LogSessionNodeHasWrongClass(nodeId, readNode.NodeClass, NodeClass.Variable);
                return null;
            }

            var browserParent = BrowserFactory.GetBackwardsBrowser(Session!);
            var browseParentResults = browserParent.Browse(new NodeId(nodeId));
            if (browseParentResults is null || browseParentResults.Count != 1)
            {
                LogSessionParentNodeNotFound(nodeId);
                return null;
            }

            var sampleValue = await TryGetDataValueForVariable((VariableNode)readNode);
            var parentObject = browseParentResults[0];
            var nodeVariable = readNode.MapToNodeVariableWithValue(parentObject.NodeId.ToString(), sampleValue);

            LogSessionReadNodeVariableSucceeded(nodeId);
            return nodeVariable;
        }
        catch (Exception ex)
        {
            LogSessionReadNodeFailure(nodeId, ex.Message);
            return null;
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

    private async Task HandleChildBrowseResults(
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
                            await ReadChildNodes(childNode, level + 1, includeObjects, includeVariables, nodeObjectReadDepth);
                        }

                        currentNode.NodeObjects.Add(childNode);
                    }
                }

                break;
            case NodeClass.Variable:
                if (includeVariables)
                {
                    await HandleBrowseResultVariableNode(currentNode, result);
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

    private async Task HandleBrowseResultVariableNode(
        NodeObject node,
        ReferenceDescription referenceDescription)
    {
        var childNodeId = referenceDescription.NodeId.ToString();
        var childNode = Session!.ReadNode(childNodeId);
        var sampleValue = await TryGetDataValueForVariable((VariableNode)childNode);
        var nodeVariable = childNode.MapToNodeVariableWithValue(node.NodeId, sampleValue);
        if (nodeVariable is not null)
        {
            node.NodeVariables.Add(nodeVariable);
        }
    }

    private async Task<DataValue?> TryGetDataValueForVariable(
        VariableNode node,
        bool loadComplexTypeSystem = false)
    {
        DataValue? sampleValue = null;

        var nodesToRead = new ReadValueIdCollection
        {
            new()
            {
                NodeId = node.NodeId,
                AttributeId = Attributes.Value,
            },
        };

        if (loadComplexTypeSystem)
        {
            var complexTypeSystem = new ComplexTypeSystem(Session!);
            var nodeDataType = node.DataType;
            LogLoadingComplexTypeSystem(node.NodeId.ToString(), nodeDataType.ToString());
            await complexTypeSystem.LoadType(nodeDataType);
        }

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
                var statusCode = resultValues[0].StatusCode;

                if (!loadComplexTypeSystem &&
                    statusCode.Code == StatusCodes.BadDataTypeIdUnknown)
                {
                    return await TryGetDataValueForVariable(node, loadComplexTypeSystem: true);
                }

                LogSessionReadNodeVariableValueFailure(node.NodeId.ToString(), statusCode.ToString());
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
            userIdentity = new UserIdentity(new AnonymousIdentityToken());
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
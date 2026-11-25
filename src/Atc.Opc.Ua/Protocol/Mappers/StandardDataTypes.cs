namespace Atc.Opc.Ua.Protocol.Mappers;

/// <summary>
/// The built-in type and .NET type name of each standard DataType, as the library names them.
/// </summary>
/// <remarks>
/// Consumers store these names and map them to their own types, so they're fixed here rather than taken from the
/// OPC UA stack, whose own types can change between its versions. The snapshot tests in
/// <c>test/Atc.Opc.Ua.Tests/Snapshots</c> check every name.
/// </remarks>
internal static class StandardDataTypes
{
    /// <summary>
    /// The built-in type of each namespace-0 DataType that has one, by numeric id. Subtypes such as Duration or UtcTime
    /// have their base's built-in type.
    /// </summary>
    private static readonly FrozenDictionary<uint, BuiltInType> BuiltInTypes = new Dictionary<uint, BuiltInType>
    {
        [1] = BuiltInType.Boolean, // Boolean
        [2] = BuiltInType.SByte, // SByte
        [3] = BuiltInType.Byte, // Byte
        [4] = BuiltInType.Int16, // Int16
        [5] = BuiltInType.UInt16, // UInt16
        [6] = BuiltInType.Int32, // Int32
        [7] = BuiltInType.UInt32, // UInt32
        [8] = BuiltInType.Int64, // Int64
        [9] = BuiltInType.UInt64, // UInt64
        [10] = BuiltInType.Float, // Float
        [11] = BuiltInType.Double, // Double
        [12] = BuiltInType.String, // String
        [13] = BuiltInType.DateTime, // DateTime
        [14] = BuiltInType.Guid, // Guid
        [15] = BuiltInType.ByteString, // ByteString
        [16] = BuiltInType.XmlElement, // XmlElement
        [17] = BuiltInType.NodeId, // NodeId
        [18] = BuiltInType.ExpandedNodeId, // ExpandedNodeId
        [19] = BuiltInType.StatusCode, // StatusCode
        [20] = BuiltInType.QualifiedName, // QualifiedName
        [21] = BuiltInType.LocalizedText, // LocalizedText
        [22] = BuiltInType.ExtensionObject, // Structure
        [23] = BuiltInType.DataValue, // DataValue
        [24] = BuiltInType.Variant, // BaseDataType
        [25] = BuiltInType.DiagnosticInfo, // DiagnosticInfo
        [29] = BuiltInType.Enumeration, // Enumeration
        [30] = BuiltInType.ByteString, // Image
        [288] = BuiltInType.UInt32, // IntegerId
        [289] = BuiltInType.UInt32, // Counter
        [290] = BuiltInType.Double, // Duration
        [291] = BuiltInType.String, // NumericRange
        [294] = BuiltInType.DateTime, // UtcTime
        [295] = BuiltInType.String, // LocaleId
        [311] = BuiltInType.ByteString, // ApplicationInstanceCertificate
        [388] = BuiltInType.NodeId, // SessionAuthenticationToken
        [521] = BuiltInType.ByteString, // ContinuationPoint
        [2000] = BuiltInType.ByteString, // ImageBMP
        [2001] = BuiltInType.ByteString, // ImageGIF
        [2002] = BuiltInType.ByteString, // ImageJPG
        [2003] = BuiltInType.ByteString, // ImagePNG
        [11737] = BuiltInType.UInt64, // BitFieldMaskDataType
        [12877] = BuiltInType.String, // NormalizedString
        [12878] = BuiltInType.String, // DecimalString
        [12879] = BuiltInType.String, // DurationString
        [12880] = BuiltInType.String, // TimeString
        [12881] = BuiltInType.String, // DateString
        [16307] = BuiltInType.ByteString, // AudioDataType
        [17588] = BuiltInType.UInt32, // Index
        [20998] = BuiltInType.UInt32, // VersionTime
    }.ToFrozenDictionary();

    /// <summary>
    /// The .NET type name of a scalar of each namespace-0 DataType, by numeric id. A DataType that isn't listed is
    /// <c>object</c>.
    /// </summary>
    private static readonly FrozenDictionary<uint, string> ScalarTypeNames = new Dictionary<uint, string>
    {
        [1] = "bool", // Boolean
        [2] = "sbyte", // SByte
        [3] = "byte", // Byte
        [4] = "short", // Int16
        [5] = "ushort", // UInt16
        [6] = "int", // Int32
        [7] = "uint", // UInt32
        [8] = "long", // Int64
        [9] = "ulong", // UInt64
        [10] = "float", // Float
        [11] = "double", // Double
        [12] = "string", // String
        [13] = "DateTime", // DateTime
        [14] = "Uuid", // Guid
        [15] = "byte[]", // ByteString
        [16] = "XmlElement", // XmlElement
        [17] = "NodeId", // NodeId
        [18] = "ExpandedNodeId", // ExpandedNodeId
        [19] = "StatusCode", // StatusCode
        [20] = "QualifiedName", // QualifiedName
        [21] = "LocalizedText", // LocalizedText
        [22] = "ExtensionObject", // Structure
        [23] = "DataValue", // DataValue
        [24] = "Variant", // BaseDataType
        [25] = "DiagnosticInfo", // DiagnosticInfo
        [26] = "Variant", // Number
        [27] = "Variant", // Integer
        [28] = "Variant", // UInteger
        [29] = "int", // Enumeration
        [30] = "byte[]", // Image
        [96] = "RolePermissionType", // RolePermissionType
        [97] = "DataTypeDefinition", // DataTypeDefinition
        [99] = "StructureDefinition", // StructureDefinition
        [100] = "EnumDefinition", // EnumDefinition
        [101] = "StructureField", // StructureField
        [102] = "EnumField", // EnumField
        [258] = "Node", // Node
        [261] = "ObjectNode", // ObjectNode
        [264] = "ObjectTypeNode", // ObjectTypeNode
        [267] = "VariableNode", // VariableNode
        [270] = "VariableTypeNode", // VariableTypeNode
        [273] = "ReferenceTypeNode", // ReferenceTypeNode
        [276] = "MethodNode", // MethodNode
        [279] = "ViewNode", // ViewNode
        [282] = "DataTypeNode", // DataTypeNode
        [285] = "ReferenceNode", // ReferenceNode
        [288] = "uint", // IntegerId
        [289] = "uint", // Counter
        [290] = "double", // Duration
        [291] = "string", // NumericRange
        [294] = "DateTime", // UtcTime
        [295] = "string", // LocaleId
        [296] = "Argument", // Argument
        [299] = "StatusResult", // StatusResult
        [304] = "UserTokenPolicy", // UserTokenPolicy
        [308] = "ApplicationDescription", // ApplicationDescription
        [311] = "byte[]", // ApplicationInstanceCertificate
        [312] = "EndpointDescription", // EndpointDescription
        [316] = "UserIdentityToken", // UserIdentityToken
        [319] = "AnonymousIdentityToken", // AnonymousIdentityToken
        [322] = "UserNameIdentityToken", // UserNameIdentityToken
        [325] = "X509IdentityToken", // X509IdentityToken
        [331] = "EndpointConfiguration", // EndpointConfiguration
        [338] = "BuildInfo", // BuildInfo
        [344] = "SignedSoftwareCertificate", // SignedSoftwareCertificate
        [349] = "NodeAttributes", // NodeAttributes
        [352] = "ObjectAttributes", // ObjectAttributes
        [355] = "VariableAttributes", // VariableAttributes
        [358] = "MethodAttributes", // MethodAttributes
        [361] = "ObjectTypeAttributes", // ObjectTypeAttributes
        [364] = "VariableTypeAttributes", // VariableTypeAttributes
        [367] = "ReferenceTypeAttributes", // ReferenceTypeAttributes
        [370] = "DataTypeAttributes", // DataTypeAttributes
        [373] = "ViewAttributes", // ViewAttributes
        [376] = "AddNodesItem", // AddNodesItem
        [379] = "AddReferencesItem", // AddReferencesItem
        [382] = "DeleteNodesItem", // DeleteNodesItem
        [385] = "DeleteReferencesItem", // DeleteReferencesItem
        [388] = "NodeId", // SessionAuthenticationToken
        [389] = "RequestHeader", // RequestHeader
        [392] = "ResponseHeader", // ResponseHeader
        [395] = "ServiceFault", // ServiceFault
        [420] = "FindServersRequest", // FindServersRequest
        [423] = "FindServersResponse", // FindServersResponse
        [426] = "GetEndpointsRequest", // GetEndpointsRequest
        [429] = "GetEndpointsResponse", // GetEndpointsResponse
        [432] = "RegisteredServer", // RegisteredServer
        [435] = "RegisterServerRequest", // RegisterServerRequest
        [438] = "RegisterServerResponse", // RegisterServerResponse
        [441] = "ChannelSecurityToken", // ChannelSecurityToken
        [444] = "OpenSecureChannelRequest", // OpenSecureChannelRequest
        [447] = "OpenSecureChannelResponse", // OpenSecureChannelResponse
        [450] = "CloseSecureChannelRequest", // CloseSecureChannelRequest
        [453] = "CloseSecureChannelResponse", // CloseSecureChannelResponse
        [456] = "SignatureData", // SignatureData
        [459] = "CreateSessionRequest", // CreateSessionRequest
        [462] = "CreateSessionResponse", // CreateSessionResponse
        [465] = "ActivateSessionRequest", // ActivateSessionRequest
        [468] = "ActivateSessionResponse", // ActivateSessionResponse
        [471] = "CloseSessionRequest", // CloseSessionRequest
        [474] = "CloseSessionResponse", // CloseSessionResponse
        [477] = "CancelRequest", // CancelRequest
        [480] = "CancelResponse", // CancelResponse
        [483] = "AddNodesResult", // AddNodesResult
        [486] = "AddNodesRequest", // AddNodesRequest
        [489] = "AddNodesResponse", // AddNodesResponse
        [492] = "AddReferencesRequest", // AddReferencesRequest
        [495] = "AddReferencesResponse", // AddReferencesResponse
        [498] = "DeleteNodesRequest", // DeleteNodesRequest
        [501] = "DeleteNodesResponse", // DeleteNodesResponse
        [504] = "DeleteReferencesRequest", // DeleteReferencesRequest
        [507] = "DeleteReferencesResponse", // DeleteReferencesResponse
        [511] = "ViewDescription", // ViewDescription
        [514] = "BrowseDescription", // BrowseDescription
        [518] = "ReferenceDescription", // ReferenceDescription
        [521] = "byte[]", // ContinuationPoint
        [522] = "BrowseResult", // BrowseResult
        [525] = "BrowseRequest", // BrowseRequest
        [528] = "BrowseResponse", // BrowseResponse
        [531] = "BrowseNextRequest", // BrowseNextRequest
        [534] = "BrowseNextResponse", // BrowseNextResponse
        [537] = "RelativePathElement", // RelativePathElement
        [540] = "RelativePath", // RelativePath
        [543] = "BrowsePath", // BrowsePath
        [546] = "BrowsePathTarget", // BrowsePathTarget
        [549] = "BrowsePathResult", // BrowsePathResult
        [552] = "TranslateBrowsePathsToNodeIdsRequest", // TranslateBrowsePathsToNodeIdsRequest
        [555] = "TranslateBrowsePathsToNodeIdsResponse", // TranslateBrowsePathsToNodeIdsResponse
        [558] = "RegisterNodesRequest", // RegisterNodesRequest
        [561] = "RegisterNodesResponse", // RegisterNodesResponse
        [564] = "UnregisterNodesRequest", // UnregisterNodesRequest
        [567] = "UnregisterNodesResponse", // UnregisterNodesResponse
        [570] = "QueryDataDescription", // QueryDataDescription
        [573] = "NodeTypeDescription", // NodeTypeDescription
        [577] = "QueryDataSet", // QueryDataSet
        [580] = "NodeReference", // NodeReference
        [583] = "ContentFilterElement", // ContentFilterElement
        [586] = "ContentFilter", // ContentFilter
        [589] = "FilterOperand", // FilterOperand
        [592] = "ElementOperand", // ElementOperand
        [595] = "LiteralOperand", // LiteralOperand
        [598] = "AttributeOperand", // AttributeOperand
        [601] = "SimpleAttributeOperand", // SimpleAttributeOperand
        [604] = "ContentFilterElementResult", // ContentFilterElementResult
        [607] = "ContentFilterResult", // ContentFilterResult
        [610] = "ParsingResult", // ParsingResult
        [613] = "QueryFirstRequest", // QueryFirstRequest
        [616] = "QueryFirstResponse", // QueryFirstResponse
        [619] = "QueryNextRequest", // QueryNextRequest
        [622] = "QueryNextResponse", // QueryNextResponse
        [626] = "ReadValueId", // ReadValueId
        [629] = "ReadRequest", // ReadRequest
        [632] = "ReadResponse", // ReadResponse
        [635] = "HistoryReadValueId", // HistoryReadValueId
        [638] = "HistoryReadResult", // HistoryReadResult
        [641] = "HistoryReadDetails", // HistoryReadDetails
        [644] = "ReadEventDetails", // ReadEventDetails
        [647] = "ReadRawModifiedDetails", // ReadRawModifiedDetails
        [650] = "ReadProcessedDetails", // ReadProcessedDetails
        [653] = "ReadAtTimeDetails", // ReadAtTimeDetails
        [656] = "HistoryData", // HistoryData
        [659] = "HistoryEvent", // HistoryEvent
        [662] = "HistoryReadRequest", // HistoryReadRequest
        [665] = "HistoryReadResponse", // HistoryReadResponse
        [668] = "WriteValue", // WriteValue
        [671] = "WriteRequest", // WriteRequest
        [674] = "WriteResponse", // WriteResponse
        [677] = "HistoryUpdateDetails", // HistoryUpdateDetails
        [680] = "UpdateDataDetails", // UpdateDataDetails
        [683] = "UpdateEventDetails", // UpdateEventDetails
        [686] = "DeleteRawModifiedDetails", // DeleteRawModifiedDetails
        [689] = "DeleteAtTimeDetails", // DeleteAtTimeDetails
        [692] = "DeleteEventDetails", // DeleteEventDetails
        [695] = "HistoryUpdateResult", // HistoryUpdateResult
        [698] = "HistoryUpdateRequest", // HistoryUpdateRequest
        [701] = "HistoryUpdateResponse", // HistoryUpdateResponse
        [704] = "CallMethodRequest", // CallMethodRequest
        [707] = "CallMethodResult", // CallMethodResult
        [710] = "CallRequest", // CallRequest
        [713] = "CallResponse", // CallResponse
        [719] = "MonitoringFilter", // MonitoringFilter
        [722] = "DataChangeFilter", // DataChangeFilter
        [725] = "EventFilter", // EventFilter
        [728] = "AggregateFilter", // AggregateFilter
        [731] = "MonitoringFilterResult", // MonitoringFilterResult
        [734] = "EventFilterResult", // EventFilterResult
        [737] = "AggregateFilterResult", // AggregateFilterResult
        [740] = "MonitoringParameters", // MonitoringParameters
        [743] = "MonitoredItemCreateRequest", // MonitoredItemCreateRequest
        [746] = "MonitoredItemCreateResult", // MonitoredItemCreateResult
        [749] = "CreateMonitoredItemsRequest", // CreateMonitoredItemsRequest
        [752] = "CreateMonitoredItemsResponse", // CreateMonitoredItemsResponse
        [755] = "MonitoredItemModifyRequest", // MonitoredItemModifyRequest
        [758] = "MonitoredItemModifyResult", // MonitoredItemModifyResult
        [761] = "ModifyMonitoredItemsRequest", // ModifyMonitoredItemsRequest
        [764] = "ModifyMonitoredItemsResponse", // ModifyMonitoredItemsResponse
        [767] = "SetMonitoringModeRequest", // SetMonitoringModeRequest
        [770] = "SetMonitoringModeResponse", // SetMonitoringModeResponse
        [773] = "SetTriggeringRequest", // SetTriggeringRequest
        [776] = "SetTriggeringResponse", // SetTriggeringResponse
        [779] = "DeleteMonitoredItemsRequest", // DeleteMonitoredItemsRequest
        [782] = "DeleteMonitoredItemsResponse", // DeleteMonitoredItemsResponse
        [785] = "CreateSubscriptionRequest", // CreateSubscriptionRequest
        [788] = "CreateSubscriptionResponse", // CreateSubscriptionResponse
        [791] = "ModifySubscriptionRequest", // ModifySubscriptionRequest
        [794] = "ModifySubscriptionResponse", // ModifySubscriptionResponse
        [797] = "SetPublishingModeRequest", // SetPublishingModeRequest
        [800] = "SetPublishingModeResponse", // SetPublishingModeResponse
        [803] = "NotificationMessage", // NotificationMessage
        [806] = "MonitoredItemNotification", // MonitoredItemNotification
        [809] = "DataChangeNotification", // DataChangeNotification
        [818] = "StatusChangeNotification", // StatusChangeNotification
        [821] = "SubscriptionAcknowledgement", // SubscriptionAcknowledgement
        [824] = "PublishRequest", // PublishRequest
        [827] = "PublishResponse", // PublishResponse
        [830] = "RepublishRequest", // RepublishRequest
        [833] = "RepublishResponse", // RepublishResponse
        [836] = "TransferResult", // TransferResult
        [839] = "TransferSubscriptionsRequest", // TransferSubscriptionsRequest
        [842] = "TransferSubscriptionsResponse", // TransferSubscriptionsResponse
        [845] = "DeleteSubscriptionsRequest", // DeleteSubscriptionsRequest
        [848] = "DeleteSubscriptionsResponse", // DeleteSubscriptionsResponse
        [853] = "RedundantServerDataType", // RedundantServerDataType
        [856] = "SamplingIntervalDiagnosticsDataType", // SamplingIntervalDiagnosticsDataType
        [859] = "ServerDiagnosticsSummaryDataType", // ServerDiagnosticsSummaryDataType
        [862] = "ServerStatusDataType", // ServerStatusDataType
        [865] = "SessionDiagnosticsDataType", // SessionDiagnosticsDataType
        [868] = "SessionSecurityDiagnosticsDataType", // SessionSecurityDiagnosticsDataType
        [871] = "ServiceCounterDataType", // ServiceCounterDataType
        [874] = "SubscriptionDiagnosticsDataType", // SubscriptionDiagnosticsDataType
        [877] = "ModelChangeStructureDataType", // ModelChangeStructureDataType
        [884] = "Range", // Range
        [887] = "EUInformation", // EUInformation
        [891] = "Annotation", // Annotation
        [894] = "ProgramDiagnosticDataType", // ProgramDiagnosticDataType
        [897] = "SemanticChangeStructureDataType", // SemanticChangeStructureDataType
        [914] = "EventNotificationList", // EventNotificationList
        [917] = "EventFieldList", // EventFieldList
        [920] = "HistoryEventFieldList", // HistoryEventFieldList
        [938] = "IssuedIdentityToken", // IssuedIdentityToken
        [945] = "NotificationData", // NotificationData
        [948] = "AggregateConfiguration", // AggregateConfiguration
        [2000] = "byte[]", // ImageBMP
        [2001] = "byte[]", // ImageGIF
        [2002] = "byte[]", // ImageJPG
        [2003] = "byte[]", // ImagePNG
        [7594] = "EnumValueType", // EnumValueType
        [8912] = "TimeZoneDataType", // TimeZoneDataType
        [11216] = "ModificationInfo", // ModificationInfo
        [11217] = "HistoryModifiedData", // HistoryModifiedData
        [11295] = "UpdateStructureDataDetails", // UpdateStructureDataDetails
        [11737] = "ulong", // BitFieldMaskDataType
        [11879] = "InstanceNode", // InstanceNode
        [11880] = "TypeNode", // TypeNode
        [11943] = "EndpointUrlListDataType", // EndpointUrlListDataType
        [11944] = "NetworkGroupDataType", // NetworkGroupDataType
        [12079] = "AxisInformation", // AxisInformation
        [12080] = "XVType", // XVType
        [12171] = "ComplexNumberType", // ComplexNumberType
        [12172] = "DoubleComplexNumberType", // DoubleComplexNumberType
        [12189] = "ServerOnNetwork", // ServerOnNetwork
        [12190] = "FindServersOnNetworkRequest", // FindServersOnNetworkRequest
        [12191] = "FindServersOnNetworkResponse", // FindServersOnNetworkResponse
        [12193] = "RegisterServer2Request", // RegisterServer2Request
        [12194] = "RegisterServer2Response", // RegisterServer2Response
        [12554] = "TrustListDataType", // TrustListDataType
        [12755] = "OptionSet", // OptionSet
        [12756] = "Union", // Union
        [12877] = "string", // NormalizedString
        [12878] = "string", // DecimalString
        [12879] = "string", // DurationString
        [12880] = "string", // TimeString
        [12881] = "string", // DateString
        [12890] = "DiscoveryConfiguration", // DiscoveryConfiguration
        [12891] = "MdnsDiscoveryConfiguration", // MdnsDiscoveryConfiguration
        [14273] = "PublishedVariableDataType", // PublishedVariableDataType
        [14523] = "DataSetMetaDataType", // DataSetMetaDataType
        [14524] = "FieldMetaData", // FieldMetaData
        [14525] = "DataTypeDescription", // DataTypeDescription
        [14533] = "KeyValuePair", // KeyValuePair
        [14593] = "ConfigurationVersionDataType", // ConfigurationVersionDataType
        [14744] = "FieldTargetDataType", // FieldTargetDataType
        [15005] = "SimpleTypeDescription", // SimpleTypeDescription
        [15006] = "UABinaryFileDataType", // UABinaryFileDataType
        [15007] = "BrokerConnectionTransportDataType", // BrokerConnectionTransportDataType
        [15434] = "BaseConfigurationDataType", // BaseConfigurationDataType
        [15435] = "BaseConfigurationRecordDataType", // BaseConfigurationRecordDataType
        [15436] = "CertificateGroupDataType", // CertificateGroupDataType
        [15480] = "WriterGroupDataType", // WriterGroupDataType
        [15487] = "StructureDescription", // StructureDescription
        [15488] = "EnumDescription", // EnumDescription
        [15502] = "NetworkAddressDataType", // NetworkAddressDataType
        [15510] = "NetworkAddressUrlDataType", // NetworkAddressUrlDataType
        [15520] = "ReaderGroupDataType", // ReaderGroupDataType
        [15528] = "EndpointType", // EndpointType
        [15530] = "PubSubConfigurationDataType", // PubSubConfigurationDataType
        [15532] = "DatagramWriterGroupTransportDataType", // DatagramWriterGroupTransportDataType
        [15534] = "DataTypeSchemaHeader", // DataTypeSchemaHeader
        [15538] = "ConfigurationUpdateTargetType", // ConfigurationUpdateTargetType
        [15549] = "ApplicationConfigurationDataType", // ApplicationConfigurationDataType
        [15556] = "ApplicationIdentityDataType", // ApplicationIdentityDataType
        [15557] = "EndpointDataType", // EndpointDataType
        [15558] = "ServerEndpointDataType", // ServerEndpointDataType
        [15559] = "SecuritySettingsDataType", // SecuritySettingsDataType
        [15560] = "UserTokenSettingsDataType", // UserTokenSettingsDataType
        [15578] = "PublishedDataSetDataType", // PublishedDataSetDataType
        [15580] = "PublishedDataSetSourceDataType", // PublishedDataSetSourceDataType
        [15581] = "PublishedDataItemsDataType", // PublishedDataItemsDataType
        [15582] = "PublishedEventsDataType", // PublishedEventsDataType
        [15597] = "DataSetWriterDataType", // DataSetWriterDataType
        [15598] = "DataSetWriterTransportDataType", // DataSetWriterTransportDataType
        [15605] = "DataSetWriterMessageDataType", // DataSetWriterMessageDataType
        [15609] = "PubSubGroupDataType", // PubSubGroupDataType
        [15611] = "WriterGroupTransportDataType", // WriterGroupTransportDataType
        [15616] = "WriterGroupMessageDataType", // WriterGroupMessageDataType
        [15617] = "PubSubConnectionDataType", // PubSubConnectionDataType
        [15618] = "ConnectionTransportDataType", // ConnectionTransportDataType
        [15621] = "ReaderGroupTransportDataType", // ReaderGroupTransportDataType
        [15622] = "ReaderGroupMessageDataType", // ReaderGroupMessageDataType
        [15623] = "DataSetReaderDataType", // DataSetReaderDataType
        [15628] = "DataSetReaderTransportDataType", // DataSetReaderTransportDataType
        [15629] = "DataSetReaderMessageDataType", // DataSetReaderMessageDataType
        [15630] = "SubscribedDataSetDataType", // SubscribedDataSetDataType
        [15631] = "TargetVariablesDataType", // TargetVariablesDataType
        [15634] = "IdentityMappingRuleType", // IdentityMappingRuleType
        [15635] = "SubscribedDataSetMirrorDataType", // SubscribedDataSetMirrorDataType
        [15645] = "UadpWriterGroupMessageDataType", // UadpWriterGroupMessageDataType
        [15652] = "UadpDataSetWriterMessageDataType", // UadpDataSetWriterMessageDataType
        [15653] = "UadpDataSetReaderMessageDataType", // UadpDataSetReaderMessageDataType
        [15657] = "JsonWriterGroupMessageDataType", // JsonWriterGroupMessageDataType
        [15664] = "JsonDataSetWriterMessageDataType", // JsonDataSetWriterMessageDataType
        [15665] = "JsonDataSetReaderMessageDataType", // JsonDataSetReaderMessageDataType
        [15667] = "BrokerWriterGroupTransportDataType", // BrokerWriterGroupTransportDataType
        [15669] = "BrokerDataSetWriterTransportDataType", // BrokerDataSetWriterTransportDataType
        [15670] = "BrokerDataSetReaderTransportDataType", // BrokerDataSetReaderTransportDataType
        [15901] = "SessionlessInvokeRequestType", // SessionlessInvokeRequestType
        [16307] = "byte[]", // AudioDataType
        [16313] = "AdditionalParametersType", // AdditionalParametersType
        [17467] = "DatagramConnectionTransportDataType", // DatagramConnectionTransportDataType
        [17548] = "EphemeralKeyType", // EphemeralKeyType
        [17588] = "uint", // Index
        [17606] = "GenericAttributeValue", // GenericAttributeValue
        [17607] = "GenericAttributes", // GenericAttributes
        [17861] = "DecimalDataType", // DecimalDataType
        [18593] = "ActionTargetDataType", // ActionTargetDataType
        [18594] = "PublishedActionDataType", // PublishedActionDataType
        [18597] = "ActionMethodDataType", // ActionMethodDataType
        [18648] = "SortRuleElement", // SortRuleElement
        [18649] = "ReadEventDetailsSorted", // ReadEventDetailsSorted
        [18793] = "PublishedActionMethodDataType", // PublishedActionMethodDataType
        [18794] = "DtlsPubSubConnectionDataType", // DtlsPubSubConnectionDataType
        [18806] = "RationalNumber", // RationalNumber
        [18807] = "Vector", // Vector
        [18808] = "ThreeDVector", // ThreeDVector
        [18809] = "CartesianCoordinates", // CartesianCoordinates
        [18810] = "ThreeDCartesianCoordinates", // ThreeDCartesianCoordinates
        [18811] = "Orientation", // Orientation
        [18812] = "ThreeDOrientation", // ThreeDOrientation
        [18813] = "Frame", // Frame
        [18814] = "ThreeDFrame", // ThreeDFrame
        [18953] = "LldpManagementAddressTxPortType", // LldpManagementAddressTxPortType
        [18954] = "LldpManagementAddressType", // LldpManagementAddressType
        [18955] = "LldpTlvType", // LldpTlvType
        [19311] = "JsonNetworkMessage", // JsonNetworkMessage
        [19312] = "JsonDataSetMessage", // JsonDataSetMessage
        [19313] = "JsonDataSetMetaDataMessage", // JsonDataSetMetaDataMessage
        [19314] = "JsonApplicationDescriptionMessage", // JsonApplicationDescriptionMessage
        [19315] = "JsonServerEndpointsMessage", // JsonServerEndpointsMessage
        [19316] = "JsonStatusMessage", // JsonStatusMessage
        [19317] = "JsonPubSubConnectionMessage", // JsonPubSubConnectionMessage
        [19318] = "JsonActionMetaDataMessage", // JsonActionMetaDataMessage
        [19319] = "JsonActionResponderMessage", // JsonActionResponderMessage
        [19320] = "JsonActionNetworkMessage", // JsonActionNetworkMessage
        [19321] = "JsonActionRequestMessage", // JsonActionRequestMessage
        [19322] = "JsonActionResponseMessage", // JsonActionResponseMessage
        [19361] = "LogRecord", // LogRecord
        [19445] = "AuthorizationServiceConfigurationDataType", // AuthorizationServiceConfigurationDataType
        [19745] = "LogRecordsDataType", // LogRecordsDataType
        [19746] = "SpanContextDataType", // SpanContextDataType
        [19747] = "TraceContextDataType", // TraceContextDataType
        [19748] = "NameValuePair", // NameValuePair
        [20998] = "uint", // VersionTime
        [20999] = "SessionlessInvokeResponseType", // SessionlessInvokeResponseType
        [23468] = "AliasNameDataType", // AliasNameDataType
        [23497] = "ReadAnnotationDataDetails", // ReadAnnotationDataDetails
        [23498] = "CurrencyUnitType", // CurrencyUnitType
        [23599] = "StandaloneSubscribedDataSetRefDataType", // StandaloneSubscribedDataSetRefDataType
        [23600] = "StandaloneSubscribedDataSetDataType", // StandaloneSubscribedDataSetDataType
        [23601] = "SecurityGroupDataType", // SecurityGroupDataType
        [23602] = "PubSubConfiguration2DataType", // PubSubConfiguration2DataType
        [23603] = "QosDataType", // QosDataType
        [23604] = "TransmitQosDataType", // TransmitQosDataType
        [23605] = "TransmitQosPriorityDataType", // TransmitQosPriorityDataType
        [23608] = "ReceiveQosDataType", // ReceiveQosDataType
        [23609] = "ReceiveQosPriorityDataType", // ReceiveQosPriorityDataType
        [23612] = "DatagramConnectionTransport2DataType", // DatagramConnectionTransport2DataType
        [23613] = "DatagramWriterGroupTransport2DataType", // DatagramWriterGroupTransport2DataType
        [23614] = "DatagramDataSetReaderTransportDataType", // DatagramDataSetReaderTransportDataType
        [24033] = "ProgramDiagnostic2DataType", // ProgramDiagnostic2DataType
        [24105] = "PortableQualifiedName", // PortableQualifiedName
        [24106] = "PortableNodeId", // PortableNodeId
        [24107] = "UnsignedRationalNumber", // UnsignedRationalNumber
        [24281] = "UserManagementDataType", // UserManagementDataType
        [25220] = "PriorityMappingEntryType", // PriorityMappingEntryType
        [25269] = "PublishedDataSetCustomSourceDataType", // PublishedDataSetCustomSourceDataType
        [25270] = "PubSubKeyPushTargetDataType", // PubSubKeyPushTargetDataType
        [25519] = "PubSubConfigurationRefDataType", // PubSubConfigurationRefDataType
        [25520] = "PubSubConfigurationValueDataType", // PubSubConfigurationValueDataType
        [32285] = "TransactionErrorType", // TransactionErrorType
        [32421] = "BitFieldDefinition", // BitFieldDefinition
        [32434] = "AnnotationDataType", // AnnotationDataType
        [32435] = "LinearConversionDataType", // LinearConversionDataType
        [32438] = "QuantityDimension", // QuantityDimension
        [32659] = "ReferenceDescriptionDataType", // ReferenceDescriptionDataType
        [32660] = "ReferenceListEntryDataType", // ReferenceListEntryDataType
        [32799] = "ReadEventDetails2", // ReadEventDetails2
        [32824] = "HistoryModifiedEvent", // HistoryModifiedEvent
    }.ToFrozenDictionary();

    /// <summary>
    /// The .NET type names of each built-in type: a scalar, a one-dimensional array, and the element of a
    /// multi-dimensional array (null when every shape is <c>Variant</c>).
    /// </summary>
    private static readonly FrozenDictionary<BuiltInType, BuiltInTypeNames> BuiltInTypeNamesByType = new Dictionary<BuiltInType, BuiltInTypeNames>
    {
        [BuiltInType.Null] = new("Variant", "Variant", null),
        [BuiltInType.Boolean] = new("bool", "bool[]", "Boolean"),
        [BuiltInType.SByte] = new("sbyte", "sbyte[]", "SByte"),
        [BuiltInType.Byte] = new("byte", "byte[]", "Byte"),
        [BuiltInType.Int16] = new("short", "short[]", "Int16"),
        [BuiltInType.UInt16] = new("ushort", "ushort[]", "UInt16"),
        [BuiltInType.Int32] = new("int", "int[]", "Int32"),
        [BuiltInType.UInt32] = new("uint", "uint[]", "UInt32"),
        [BuiltInType.Int64] = new("long", "long[]", "Int64"),
        [BuiltInType.UInt64] = new("ulong", "ulong[]", "UInt64"),
        [BuiltInType.Float] = new("float", "float[]", "Single"),
        [BuiltInType.Double] = new("double", "double[]", "Double"),
        [BuiltInType.String] = new("string", "string[]", "String"),
        [BuiltInType.DateTime] = new("DateTime", "DateTime[]", "DateTime"),
        [BuiltInType.Guid] = new("Uuid", "Uuid[]", "Uuid"),
        [BuiltInType.ByteString] = new("byte[]", "Byte[][]", "Byte[]"),
        [BuiltInType.XmlElement] = new("XmlElement", "XmlElement[]", "XmlElement"),
        [BuiltInType.NodeId] = new("NodeId", "NodeId[]", "NodeId"),
        [BuiltInType.ExpandedNodeId] = new("ExpandedNodeId", "ExpandedNodeId[]", "ExpandedNodeId"),
        [BuiltInType.StatusCode] = new("StatusCode", "StatusCode[]", "StatusCode"),
        [BuiltInType.QualifiedName] = new("QualifiedName", "QualifiedName[]", "QualifiedName"),
        [BuiltInType.LocalizedText] = new("LocalizedText", "LocalizedText[]", "LocalizedText"),
        [BuiltInType.ExtensionObject] = new("ExtensionObject", "ExtensionObject[]", "ExtensionObject"),
        [BuiltInType.DataValue] = new("DataValue", "DataValue[]", "DataValue"),
        [BuiltInType.Variant] = new("Variant", "Variant[]", "Variant"),
        [BuiltInType.DiagnosticInfo] = new("DiagnosticInfo", "DiagnosticInfo[]", "DiagnosticInfo"),
        [BuiltInType.Number] = new("Variant", "Variant[]", "Variant"),
        [BuiltInType.Integer] = new("Variant", "Variant[]", "Variant"),
        [BuiltInType.UInteger] = new("Variant", "Variant[]", "Variant"),
        [BuiltInType.Enumeration] = new("int", "int[]", "Int32"),
    }.ToFrozenDictionary();

    /// <summary>
    /// The built-in type of a DataType, or <see cref="BuiltInType.Null"/> when it isn't a standard DataType with one.
    /// </summary>
    /// <param name="dataType">The DataType's node id.</param>
    /// <returns>The built-in type.</returns>
    public static BuiltInType GetBuiltInType(NodeId dataType)
        => TryGetStandardId(dataType, out var id) && BuiltInTypes.TryGetValue(id, out var builtInType)
            ? builtInType
            : BuiltInType.Null;

    /// <summary>
    /// The .NET type name of a scalar of a DataType: the built-in or standard structure type, or <c>object</c>.
    /// </summary>
    /// <param name="dataType">The DataType's node id.</param>
    /// <returns>The type name.</returns>
    public static string GetScalarTypeName(NodeId dataType)
        => TryGetStandardId(dataType, out var id) && ScalarTypeNames.TryGetValue(id, out var name)
            ? name
            : "object";

    /// <summary>
    /// The .NET type name of a value of a built-in type.
    /// </summary>
    /// <param name="builtInType">The built-in type.</param>
    /// <param name="valueRank">The number of array dimensions, or <see cref="ValueRanks.Scalar"/>.</param>
    /// <returns>The type name.</returns>
    public static string GetTypeName(
        BuiltInType builtInType,
        int valueRank)
    {
        var names = BuiltInTypeNamesByType.TryGetValue(builtInType, out var found)
            ? found
            : BuiltInTypeNamesByType[BuiltInType.Null];

        return valueRank switch
        {
            <= 0 => names.Scalar,
            1 => names.Array,
            _ when names.MultiDimensionalElement is null => "Variant",
            _ => $"{names.MultiDimensionalElement}[{new string(',', valueRank - 1)}]",
        };
    }

    private static bool TryGetStandardId(
        NodeId dataType,
        out uint id)
    {
        id = 0;
        return !dataType.IsNull &&
               dataType.NamespaceIndex == 0 &&
               dataType.IdType == IdType.Numeric &&
               dataType.TryGetValue(out id);
    }

    private sealed record BuiltInTypeNames(
        string Scalar,
        string Array,
        string? MultiDimensionalElement);
}
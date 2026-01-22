// ReSharper disable InvertIf
namespace Atc.Opc.Ua.Services;

/// <summary>
/// Provides functionality for reading OPC UA DataType definitions.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - By Design")]
public partial class OpcUaClient
{
    /// <summary>
    /// Asynchronously reads an enumeration DataType definition from the OPC UA server.
    /// </summary>
    /// <param name="dataTypeNodeId">The node-id of the DataType node (e.g. "i=852" for ServerState).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A tuple containing success status, the enum DataType definition, and any error message.</returns>
    public Task<(bool Succeeded, OpcUaEnumDataType? EnumDataType, string? ErrorMessage)> ReadEnumDataTypeAsync(
        string dataTypeNodeId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dataTypeNodeId))
        {
            return Task.FromResult<(bool Succeeded, OpcUaEnumDataType? EnumDataType, string? ErrorMessage)>((false, null, "Missing dataTypeNodeId."));
        }

        dataTypeNodeId = dataTypeNodeId.Trim();

        return InvokeReadEnumDataTypeAsync(dataTypeNodeId, cancellationToken);
    }

    /// <summary>
    /// Asynchronously reads multiple enumeration DataType definitions from the OPC UA server.
    /// </summary>
    /// <param name="dataTypeNodeIds">The node-ids of the DataType nodes.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A tuple containing success status, the list of enum DataType definitions, and any error message.</returns>
    public Task<(bool Succeeded, IList<OpcUaEnumDataType>? EnumDataTypes, string? ErrorMessage)> ReadEnumDataTypesAsync(
        string[] dataTypeNodeIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataTypeNodeIds);

        if (dataTypeNodeIds.Length == 0)
        {
            return Task.FromResult<(bool Succeeded, IList<OpcUaEnumDataType>? EnumDataTypes, string? ErrorMessage)>((false, null, "Missing dataTypeNodeIds."));
        }

        dataTypeNodeIds = dataTypeNodeIds.Select(id => id.Trim()).ToArray();

        return dataTypeNodeIds.Any(string.IsNullOrWhiteSpace)
            ? Task.FromResult<(bool Succeeded, IList<OpcUaEnumDataType>? EnumDataTypes, string? ErrorMessage)>((false, null, "One or more dataTypeNodeIds are invalid."))
            : InvokeReadEnumDataTypesAsync(dataTypeNodeIds, cancellationToken);
    }

    private async Task<(bool Succeeded, OpcUaEnumDataType? EnumDataType, string? ErrorMessage)> InvokeReadEnumDataTypeAsync(
        string dataTypeNodeId,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected())
        {
            LogSessionNotConnected();
            return (false, null, "Session is not connected.");
        }

        try
        {
            LogSessionReadEnumDataType(dataTypeNodeId);

            var nodeId = new NodeId(dataTypeNodeId);

            // Read the DataType node to get basic information
            var readNode = await Session!.ReadNodeAsync(nodeId, cancellationToken);
            if (readNode is null)
            {
                LogSessionNodeNotFound(dataTypeNodeId);
                return (false, null, $"Could not find node by dataTypeNodeId '{dataTypeNodeId}'");
            }

            if (readNode.NodeClass != NodeClass.DataType)
            {
                LogSessionNodeHasWrongClass(dataTypeNodeId, readNode.NodeClass, NodeClass.DataType);
                return (false, null, $"Node with dataTypeNodeId '{dataTypeNodeId}' has wrong NodeClass '{readNode.NodeClass}', expected '{nameof(NodeClass.DataType)}'");
            }

            var enumDataType = new OpcUaEnumDataType
            {
                NodeId = dataTypeNodeId,
                Name = readNode.BrowseName?.Name ?? string.Empty,
                DisplayName = readNode.DisplayName?.Text ?? string.Empty,
                Description = readNode.Description?.Text ?? string.Empty,
            };

            // Strategy 1: Try DataTypeDefinition attribute (OPC UA 1.04+)
            var definitionRead = await TryReadDataTypeDefinitionAsync(nodeId, enumDataType, cancellationToken);
            if (definitionRead)
            {
                LogSessionReadEnumDataTypeSucceeded(dataTypeNodeId, enumDataType.Members.Count);
                return (true, enumDataType, null);
            }

            // Strategy 2: Try EnumValues property (ExtensionObject[] with EnumValueType)
            var enumValuesRead = await TryReadEnumValuesPropertyAsync(nodeId, enumDataType, cancellationToken);
            if (enumValuesRead)
            {
                LogSessionReadEnumDataTypeSucceeded(dataTypeNodeId, enumDataType.Members.Count);
                return (true, enumDataType, null);
            }

            // Strategy 3: Try EnumStrings property (LocalizedText[])
            var enumStringsRead = await TryReadEnumStringsPropertyAsync(nodeId, enumDataType, cancellationToken);
            if (enumStringsRead)
            {
                LogSessionReadEnumDataTypeSucceeded(dataTypeNodeId, enumDataType.Members.Count);
                return (true, enumDataType, null);
            }

            LogSessionReadEnumDataTypeNotEnum(dataTypeNodeId);
            return (false, null, $"DataType with nodeId '{dataTypeNodeId}' is not an enumeration or has no enum definition.");
        }
        catch (Exception ex)
        {
            LogSessionReadEnumDataTypeFailure(dataTypeNodeId, ex.Message);
            return (false, null, $"Reading enum DataType with nodeId '{dataTypeNodeId}' failed: '{ex.Message}'");
        }
    }

    private async Task<(bool Succeeded, IList<OpcUaEnumDataType>? EnumDataTypes, string? ErrorMessage)> InvokeReadEnumDataTypesAsync(
        string[] dataTypeNodeIds,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected())
        {
            LogSessionNotConnected();
            return (false, null, "Session is not connected.");
        }

        var result = new List<OpcUaEnumDataType>();
        var errors = new List<string>();

        foreach (var dataTypeNodeId in dataTypeNodeIds)
        {
            var (succeeded, enumDataType, errorMessage) = await ReadEnumDataTypeAsync(dataTypeNodeId, cancellationToken);
            if (succeeded && enumDataType is not null)
            {
                result.Add(enumDataType);
            }
            else if (!string.IsNullOrEmpty(errorMessage))
            {
                errors.Add(errorMessage);
            }
        }

        return errors.Count > 0
            ? (false, result.Count > 0 ? result : null, string.Join(", ", errors))
            : (true, result, null);
    }

    /// <summary>
    /// Tries to read the DataTypeDefinition attribute (OPC UA 1.04+).
    /// </summary>
    private async Task<bool> TryReadDataTypeDefinitionAsync(
        NodeId dataTypeNodeId,
        OpcUaEnumDataType enumDataType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var nodesToRead = new ReadValueIdCollection
            {
                new ReadValueId
                {
                    NodeId = dataTypeNodeId,
                    AttributeId = Attributes.DataTypeDefinition,
                },
            };

            var readResponse = await Session!.ReadAsync(
                requestHeader: null,
                0,
                TimestampsToReturn.Neither,
                nodesToRead,
                cancellationToken);

            if (readResponse?.Results is null ||
                readResponse.Results.Count == 0 ||
                StatusCode.IsBad(readResponse.Results[0].StatusCode))
            {
                return false;
            }

            var definition = readResponse.Results[0].Value;
            if (definition is ExtensionObject { Body: EnumDefinition enumDefinition })
            {
                enumDataType.HasEnumValues = true;

                foreach (var field in enumDefinition.Fields)
                {
                    enumDataType.Members.Add(new OpcUaEnumMember
                    {
                        Value = field.Value,
                        Name = field.Name ?? string.Empty,
                        DisplayName = field.DisplayName?.Text ?? field.Name ?? string.Empty,
                        Description = field.Description?.Text ?? string.Empty,
                    });
                }

                return enumDataType.Members.Count > 0;
            }
        }
        catch (Exception ex)
        {
            LogSessionReadDataTypeDefinitionFailed(ex, dataTypeNodeId.ToString());
        }

        return false;
    }

    /// <summary>
    /// Tries to read the EnumValues property (ExtensionObject[] containing EnumValueType).
    /// This is used by enums like SimaticOperatingState.
    /// </summary>
    private async Task<bool> TryReadEnumValuesPropertyAsync(
        NodeId dataTypeNodeId,
        OpcUaEnumDataType enumDataType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Browse for the EnumValues property
            var enumValuesNodeId = await FindPropertyNodeIdAsync(dataTypeNodeId, "EnumValues", cancellationToken);
            if (enumValuesNodeId is null)
            {
                return false;
            }

            // Read the EnumValues property value
            var nodesToRead = new ReadValueIdCollection
            {
                new ReadValueId
                {
                    NodeId = enumValuesNodeId,
                    AttributeId = Attributes.Value,
                },
            };

            var readResponse = await Session!.ReadAsync(
                requestHeader: null,
                0,
                TimestampsToReturn.Neither,
                nodesToRead,
                cancellationToken);

            if (readResponse?.Results is null ||
                readResponse.Results.Count == 0 ||
                StatusCode.IsBad(readResponse.Results[0].StatusCode))
            {
                return false;
            }

            var value = readResponse.Results[0].Value;

            // EnumValues is an array of ExtensionObjects containing EnumValueType
            if (value is ExtensionObject[] extensionObjects)
            {
                enumDataType.HasEnumValues = true;

                foreach (var extObj in extensionObjects)
                {
                    if (extObj.Body is EnumValueType enumValue)
                    {
                        enumDataType.Members.Add(new OpcUaEnumMember
                        {
                            Value = enumValue.Value,
                            Name = enumValue.DisplayName?.Text ?? string.Empty,
                            DisplayName = enumValue.DisplayName?.Text ?? string.Empty,
                            Description = enumValue.Description?.Text ?? string.Empty,
                        });
                    }
                }

                return enumDataType.Members.Count > 0;
            }
        }
        catch (Exception ex)
        {
            LogSessionReadEnumValuesFailed(ex, dataTypeNodeId.ToString());
        }

        return false;
    }

    /// <summary>
    /// Tries to read the EnumStrings property (LocalizedText[]).
    /// This is used by standard OPC UA enums like ServerState.
    /// </summary>
    private async Task<bool> TryReadEnumStringsPropertyAsync(
        NodeId dataTypeNodeId,
        OpcUaEnumDataType enumDataType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Browse for the EnumStrings property
            var enumStringsNodeId = await FindPropertyNodeIdAsync(dataTypeNodeId, "EnumStrings", cancellationToken);
            if (enumStringsNodeId is null)
            {
                return false;
            }

            // Read the EnumStrings property value
            var nodesToRead = new ReadValueIdCollection
            {
                new ReadValueId
                {
                    NodeId = enumStringsNodeId,
                    AttributeId = Attributes.Value,
                },
            };

            var readResponse = await Session!.ReadAsync(
                requestHeader: null,
                0,
                TimestampsToReturn.Neither,
                nodesToRead,
                cancellationToken);

            if (readResponse?.Results is null ||
                readResponse.Results.Count == 0 ||
                StatusCode.IsBad(readResponse.Results[0].StatusCode))
            {
                return false;
            }

            var value = readResponse.Results[0].Value;

            // EnumStrings is an array of LocalizedText where index = enum value
            if (value is LocalizedText[] localizedTexts)
            {
                enumDataType.HasEnumValues = false;

                for (var i = 0; i < localizedTexts.Length; i++)
                {
                    var text = localizedTexts[i].Text ?? string.Empty;
                    enumDataType.Members.Add(new OpcUaEnumMember
                    {
                        Value = i,
                        Name = text,
                        DisplayName = text,
                        Description = string.Empty,
                    });
                }

                return enumDataType.Members.Count > 0;
            }
        }
        catch (Exception ex)
        {
            LogSessionReadEnumStringsFailed(ex, dataTypeNodeId.ToString());
        }

        return false;
    }

    /// <summary>
    /// Finds a property node by browsing for a HasProperty reference with the specified name.
    /// </summary>
    private async Task<NodeId?> FindPropertyNodeIdAsync(
        NodeId parentNodeId,
        string propertyName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var browser = new Browser(Session!)
            {
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.HasProperty,
                IncludeSubtypes = true,
                NodeClassMask = (int)NodeClass.Variable,
            };

            var references = await browser.BrowseAsync(parentNodeId, cancellationToken);
            if (references is null || references.Count == 0)
            {
                return null;
            }

            foreach (var reference in references)
            {
                if (string.Equals(reference.BrowseName?.Name, propertyName, StringComparison.Ordinal))
                {
                    return ExpandedNodeId.ToNodeId(reference.NodeId, Session!.NamespaceUris);
                }
            }
        }
        catch (Exception ex)
        {
            LogSessionBrowsePropertyFailed(ex, parentNodeId.ToString(), propertyName);
        }

        return null;
    }
}

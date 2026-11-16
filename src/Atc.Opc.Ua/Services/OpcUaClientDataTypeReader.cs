// ReSharper disable InvertIf
namespace Atc.Opc.Ua.Services;

/// <summary>
/// Provides functionality for reading OPC UA DataType definitions.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - By Design")]
public partial class OpcUaClient
{
    /// <inheritdoc/>
    public Task<OpcUaEnumDataType?> ReadEnumDataTypeAsync(
        string dataTypeNodeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataTypeNodeId);

        return InvokeReadEnumDataTypeAsync(dataTypeNodeId.Trim(), cancellationToken).NormalizeCancellation(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IList<OpcUaEnumDataType>> ReadEnumDataTypesAsync(
        string[] dataTypeNodeIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataTypeNodeIds);

        if (dataTypeNodeIds.Length == 0)
        {
            throw new ArgumentException("At least one DataType node id is needed.", nameof(dataTypeNodeIds));
        }

        if (dataTypeNodeIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("A DataType node id is missing or blank.", nameof(dataTypeNodeIds));
        }

        return InvokeReadEnumDataTypesAsync([.. dataTypeNodeIds.Select(x => x.Trim())], cancellationToken)
            .NormalizeCancellation(cancellationToken);
    }

    /// <summary>
    /// Reads an enum DataType, trying its DataTypeDefinition, then its EnumValues, then its EnumStrings.
    /// </summary>
    /// <param name="dataTypeNodeId">The DataType node, e.g. <c>i=852</c>.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The enum DataType, or <see langword="null"/> when the node doesn't exist.</returns>
    /// <exception cref="ArgumentException">The node isn't a DataType, or isn't an enumeration.</exception>
    private async Task<OpcUaEnumDataType?> InvokeReadEnumDataTypeAsync(
        string dataTypeNodeId,
        CancellationToken cancellationToken)
    {
        EnsureConnected();

        LogSessionReadEnumDataType(dataTypeNodeId);

        var readNode = await ReadNodeOrDefaultAsync(dataTypeNodeId, cancellationToken);
        if (readNode is null)
        {
            return null;
        }

        if (readNode.NodeClass != NodeClass.DataType)
        {
            throw WrongNodeClass(dataTypeNodeId, readNode.NodeClass, NodeClass.DataType, nameof(dataTypeNodeId));
        }

        var nodeId = readNode.NodeId;
        var enumDataType = new OpcUaEnumDataType
        {
            NodeId = dataTypeNodeId,
            Name = readNode.BrowseName?.Name ?? string.Empty,
            DisplayName = readNode.DisplayName?.Text ?? string.Empty,
            Description = readNode.Description?.Text ?? string.Empty,
        };

        // Strategy 1: the DataTypeDefinition attribute (OPC UA 1.04+), then 2: the EnumValues property
        // (ExtensionObject[] with EnumValueType), then 3: the EnumStrings property (LocalizedText[]).
        if (await TryReadDataTypeDefinitionAsync(nodeId, enumDataType, cancellationToken) ||
            await TryReadEnumValuesPropertyAsync(nodeId, enumDataType, cancellationToken) ||
            await TryReadEnumStringsPropertyAsync(nodeId, enumDataType, cancellationToken))
        {
            LogSessionReadEnumDataTypeSucceeded(dataTypeNodeId, enumDataType.Members.Count);
            return enumDataType;
        }

        throw new ArgumentException($"DataType '{dataTypeNodeId}' isn't an enumeration, or has no enum definition.", nameof(dataTypeNodeId));
    }

    /// <summary>
    /// Reads several enum DataTypes, one at a time.
    /// </summary>
    /// <param name="dataTypeNodeIds">The DataType nodes.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The enum DataTypes, in the order of <paramref name="dataTypeNodeIds"/>; a node that doesn't exist is left out, with a warning.</returns>
    private async Task<IList<OpcUaEnumDataType>> InvokeReadEnumDataTypesAsync(
        string[] dataTypeNodeIds,
        CancellationToken cancellationToken)
    {
        EnsureConnected();

        var result = new List<OpcUaEnumDataType>(dataTypeNodeIds.Length);
        foreach (var dataTypeNodeId in dataTypeNodeIds)
        {
            var enumDataType = await InvokeReadEnumDataTypeAsync(dataTypeNodeId, cancellationToken);
            if (enumDataType is null)
            {
                LogSessionNodeLeftOut(dataTypeNodeId);
                continue;
            }

            result.Add(enumDataType);
        }

        return result;
    }

    /// <summary>
    /// Tries to read the DataTypeDefinition attribute (OPC UA 1.04+).
    /// </summary>
    private async Task<bool> TryReadDataTypeDefinitionAsync(
        NodeId dataTypeNodeId,
        OpcUaEnumDataType enumDataType,
        CancellationToken cancellationToken = default)
    {
        var session = CurrentSession;

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

            var readResponse = await session.ReadAsync(
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
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested && !ex.IsConnectionFailure() && Session is not null)
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
        var session = CurrentSession;

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

            var readResponse = await session.ReadAsync(
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
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested && !ex.IsConnectionFailure() && Session is not null)
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
        var session = CurrentSession;

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

            var readResponse = await session.ReadAsync(
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
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested && !ex.IsConnectionFailure() && Session is not null)
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
        var session = CurrentSession;

        try
        {
            var browser = new Browser(session)
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
                    return ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris);
                }
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested && !ex.IsConnectionFailure() && Session is not null)
        {
            LogSessionBrowsePropertyFailed(ex, parentNodeId.ToString(), propertyName);
        }

        return null;
    }
}
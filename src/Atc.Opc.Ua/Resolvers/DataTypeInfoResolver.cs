// ReSharper disable InvertIf
namespace Atc.Opc.Ua.Resolvers;

/// <summary>
/// Resolves and caches OPC UA DataType information including enum definitions.
/// </summary>
/// <remarks>
/// This resolver maintains a per-session cache to avoid redundant server calls.
/// The cache should be cleared when the session is disconnected.
/// </remarks>
public sealed class DataTypeInfoResolver
{
    /// <summary>
    /// Cache of resolved DataType information, keyed by the DataType NodeId string (e.g., "i=852", "ns=3;i=3063").
    /// </summary>
    private readonly Dictionary<string, ResolvedDataTypeInfo> cache = new(StringComparer.Ordinal);

    /// <summary>
    /// Clears the resolver cache. Should be called when the session is disconnected.
    /// </summary>
    public void ClearCache() => cache.Clear();

    /// <summary>
    /// Resolves the DataType information for a given VariableNode.
    /// </summary>
    /// <param name="variableNode">The variable node containing the DataType reference.</param>
    /// <param name="session">The OPC UA session for server queries.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the OPC UA and .NET type descriptors.</returns>
    public async Task<ResolvedDataTypeInfo> ResolveAsync(
        VariableNode variableNode,
        ISession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(variableNode);
        ArgumentNullException.ThrowIfNull(session);

        var dataTypeNodeId = variableNode.DataType;
        var cacheKey = dataTypeNodeId.ToString();
        var isArray = variableNode.ArrayDimensions.Count > 0;

        // Check cache first (without array info - we handle array wrapping separately)
        if (cache.TryGetValue(cacheKey, out var cached))
        {
            return WrapIfArray(cached, isArray);
        }

        // Resolve the type
        var resolved = await ResolveDataTypeAsync(dataTypeNodeId, session, cancellationToken);

        // Cache the base type (without array wrapping)
        cache[cacheKey] = resolved;

        return WrapIfArray(resolved, isArray);
    }

    /// <summary>
    /// Wraps a scalar type as an array type if the variable has array dimensions.
    /// </summary>
    /// <remarks>
    /// The cache stores only base (scalar) types to avoid duplicating entries for the same DataType
    /// (e.g., caching both "Int32" and "Int32[]" separately). When a variable is an array, this method
    /// creates array-wrapped descriptors on-the-fly:
    /// <list type="bullet">
    ///   <item>OPC UA type: copies all properties but sets <c>IsArray = true</c></item>
    ///   <item>.NET type: sets Kind to Array, appends "[]" to type names, and stores the base type in <c>ArrayElementType</c></item>
    /// </list>
    /// </remarks>
    /// <param name="baseInfo">The resolved scalar type information.</param>
    /// <param name="isArray">Whether the variable is an array.</param>
    /// <returns>The original info if not an array; otherwise, array-wrapped descriptors.</returns>
    private static ResolvedDataTypeInfo WrapIfArray(
        ResolvedDataTypeInfo baseInfo,
        bool isArray)
    {
        if (!isArray)
        {
            return baseInfo;
        }

        var arrayOpcUa = new OpUaDataType
        {
            NodeId = baseInfo.OpcUaDataType.NodeId,
            Name = baseInfo.OpcUaDataType.Name,
            DisplayName = baseInfo.OpcUaDataType.DisplayName,
            IdentifierType = baseInfo.OpcUaDataType.IdentifierType,
            Identifier = baseInfo.OpcUaDataType.Identifier,
            Kind = baseInfo.OpcUaDataType.Kind,
            IsArray = true,
        };

        var arrayDotNet = new DotNetTypeDescriptor
        {
            Kind = DotNetTypeKind.Array,
            Name = $"{baseInfo.DotNetType.Name}[]",
            ClrTypeName = $"{baseInfo.DotNetType.ClrTypeName}[]",
            ArrayElementType = baseInfo.DotNetType,
        };

        return new ResolvedDataTypeInfo(arrayOpcUa, arrayDotNet);
    }

    private async Task<ResolvedDataTypeInfo> ResolveDataTypeAsync(
        NodeId dataTypeNodeId,
        ISession session,
        CancellationToken cancellationToken)
    {
        var nodeIdString = dataTypeNodeId.ToString();
        var builtInType = TypeInfo.GetBuiltInType(dataTypeNodeId);

        // Built-in primitive types can be resolved without server calls
        if (builtInType != BuiltInType.Null)
        {
            return CreatePrimitiveTypeInfo(dataTypeNodeId, builtInType);
        }

        // Non-built-in types require server lookup
        try
        {
            var readNode = await session.ReadNodeAsync(dataTypeNodeId, cancellationToken);
            if (readNode is null)
            {
                return CreateUnknownTypeInfo(nodeIdString);
            }

            var opcUaType = new OpUaDataType
            {
                NodeId = nodeIdString,
                Name = readNode.BrowseName?.Name ?? nodeIdString,
                DisplayName = readNode.DisplayName?.Text ?? readNode.BrowseName?.Name ?? nodeIdString,
                IdentifierType = dataTypeNodeId.IdType.GetIdentifierTypeName(),
                Identifier = dataTypeNodeId.GetIdentifierAsString(),
                Kind = OpcUaTypeKind.Unknown,
                IsArray = false,
            };

            // Try to determine if this is an enum
            var enumMembers = await TryResolveEnumMembersAsync(dataTypeNodeId, session, cancellationToken);
            if (enumMembers is not null && enumMembers.Count > 0)
            {
                opcUaType.Kind = OpcUaTypeKind.Enum;

                var dotNetEnumMembers = new List<DotNetEnumMember>(enumMembers.Count);
                foreach (var item in enumMembers)
                {
                    // OPC UA enum values are Int64 but CLR enums typically use int.
                    // If a value exceeds int range, fall back to structure handling.
                    if (item.Value < int.MinValue || item.Value > int.MaxValue)
                    {
                        opcUaType.Kind = OpcUaTypeKind.Structure;
                        return new ResolvedDataTypeInfo(opcUaType, new DotNetTypeDescriptor
                        {
                            Kind = DotNetTypeKind.Complex,
                            Name = opcUaType.Name,
                            ClrTypeName = "object",
                        });
                    }

                    dotNetEnumMembers.Add(new DotNetEnumMember
                    {
                        Value = (int)item.Value,
                        Name = item.Name,
                        DisplayName = string.IsNullOrEmpty(item.DisplayName) ? null : item.DisplayName,
                    });
                }

                var dotNetType = new DotNetTypeDescriptor
                {
                    Kind = DotNetTypeKind.Enum,
                    Name = opcUaType.Name,
                    ClrTypeName = "int", // Enums are typically backed by int
                    EnumMembers = dotNetEnumMembers,
                };

                return new ResolvedDataTypeInfo(opcUaType, dotNetType);
            }

            // Not an enum - assume structure or unknown
            opcUaType.Kind = OpcUaTypeKind.Structure;

            var structDotNetType = new DotNetTypeDescriptor
            {
                Kind = DotNetTypeKind.Complex,
                Name = opcUaType.Name,
                ClrTypeName = "object",
            };

            return new ResolvedDataTypeInfo(opcUaType, structDotNetType);
        }
        catch
        {
            return CreateUnknownTypeInfo(nodeIdString);
        }
    }

    private static ResolvedDataTypeInfo CreatePrimitiveTypeInfo(
        NodeId nodeId,
        BuiltInType builtInType)
    {
        var typeName = builtInType.ToString();
        var systemType = TypeInfo.GetSystemType(builtInType, ValueRanks.Scalar);
        var clrTypeName = systemType?.BeautifyTypeName() ?? typeName.ToLowerInvariant();

        var opcUaType = new OpUaDataType
        {
            NodeId = nodeId.ToString(),
            Name = typeName,
            DisplayName = typeName,
            IdentifierType = nodeId.IdType.GetIdentifierTypeName(),
            Identifier = nodeId.GetIdentifierAsString(),
            Kind = OpcUaTypeKind.Primitive,
            IsArray = false,
        };

        var dotNetType = new DotNetTypeDescriptor
        {
            Kind = DotNetTypeKind.Primitive,
            Name = typeName,
            ClrTypeName = clrTypeName,
        };

        return new ResolvedDataTypeInfo(opcUaType, dotNetType);
    }

    private static ResolvedDataTypeInfo CreateUnknownTypeInfo(string nodeIdString)
    {
        var opcUaType = new OpUaDataType
        {
            NodeId = nodeIdString,
            Name = nodeIdString,
            DisplayName = nodeIdString,
            IdentifierType = "Unknown",
            Identifier = string.Empty,
            Kind = OpcUaTypeKind.Unknown,
            IsArray = false,
        };

        var dotNetType = new DotNetTypeDescriptor
        {
            Kind = DotNetTypeKind.Unknown,
            Name = nodeIdString,
            ClrTypeName = "object",
        };

        return new ResolvedDataTypeInfo(opcUaType, dotNetType);
    }

    private static async Task<IList<OpcUaEnumMember>?> TryResolveEnumMembersAsync(
        NodeId dataTypeNodeId,
        ISession session,
        CancellationToken cancellationToken)
    {
        // Strategy 1: Try DataTypeDefinition attribute (OPC UA 1.04+)
        var membersFromDefinition = await TryReadDataTypeDefinitionAsync(dataTypeNodeId, session, cancellationToken);
        if (membersFromDefinition is not null)
        {
            return membersFromDefinition;
        }

        // Strategy 2: Try EnumValues property
        var membersFromEnumValues = await TryReadEnumValuesPropertyAsync(dataTypeNodeId, session, cancellationToken);
        if (membersFromEnumValues is not null)
        {
            return membersFromEnumValues;
        }

        // Strategy 3: Try EnumStrings property
        var membersFromEnumStrings = await TryReadEnumStringsPropertyAsync(dataTypeNodeId, session, cancellationToken);
        return membersFromEnumStrings;
    }

    private static async Task<IList<OpcUaEnumMember>?> TryReadDataTypeDefinitionAsync(
        NodeId dataTypeNodeId,
        ISession session,
        CancellationToken cancellationToken)
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
                return null;
            }

            var definition = readResponse.Results[0].Value;
            if (definition is ExtensionObject { Body: EnumDefinition enumDefinition })
            {
                var members = new List<OpcUaEnumMember>(enumDefinition.Fields.Count);

                foreach (var field in enumDefinition.Fields)
                {
                    members.Add(new OpcUaEnumMember
                    {
                        Value = field.Value,
                        Name = field.Name ?? string.Empty,
                        DisplayName = field.DisplayName?.Text ?? field.Name ?? string.Empty,
                        Description = field.Description?.Text ?? string.Empty,
                    });
                }

                return members;
            }
        }
        catch
        {
            // DataTypeDefinition not supported or not available
        }

        return null;
    }

    private static async Task<IList<OpcUaEnumMember>?> TryReadEnumValuesPropertyAsync(
        NodeId dataTypeNodeId,
        ISession session,
        CancellationToken cancellationToken)
    {
        try
        {
            var enumValuesNodeId = await FindPropertyNodeIdAsync(dataTypeNodeId, "EnumValues", session, cancellationToken);
            if (enumValuesNodeId is null)
            {
                return null;
            }

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
                return null;
            }

            var value = readResponse.Results[0].Value;
            if (value is ExtensionObject[] extensionObjects)
            {
                var members = new List<OpcUaEnumMember>();

                foreach (var ext in extensionObjects)
                {
                    if (ext.Body is EnumValueType enumValue)
                    {
                        members.Add(new OpcUaEnumMember
                        {
                            Value = enumValue.Value,
                            Name = enumValue.DisplayName?.Text ?? string.Empty,
                            DisplayName = enumValue.DisplayName?.Text ?? string.Empty,
                            Description = enumValue.Description?.Text ?? string.Empty,
                        });
                    }
                }

                return members;
            }
        }
        catch
        {
            // EnumValues property not available or read failed
        }

        return null;
    }

    private static async Task<IList<OpcUaEnumMember>?> TryReadEnumStringsPropertyAsync(
        NodeId dataTypeNodeId,
        ISession session,
        CancellationToken cancellationToken)
    {
        try
        {
            var enumStringsNodeId = await FindPropertyNodeIdAsync(dataTypeNodeId, "EnumStrings", session, cancellationToken);
            if (enumStringsNodeId is null)
            {
                return null;
            }

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
                return null;
            }

            var value = readResponse.Results[0].Value;
            if (value is LocalizedText[] localizedTexts)
            {
                var members = new List<OpcUaEnumMember>();

                for (var i = 0; i < localizedTexts.Length; i++)
                {
                    var text = localizedTexts[i].Text ?? string.Empty;
                    members.Add(new OpcUaEnumMember
                    {
                        Value = i,
                        Name = text,
                        DisplayName = text,
                        Description = string.Empty,
                    });
                }

                return members;
            }
        }
        catch
        {
            // EnumStrings property not available or read failed
        }

        return null;
    }

    private static async Task<NodeId?> FindPropertyNodeIdAsync(
        NodeId parentNodeId,
        string propertyName,
        ISession session,
        CancellationToken cancellationToken)
    {
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
        catch
        {
            // Browse failed
        }

        return null;
    }
}
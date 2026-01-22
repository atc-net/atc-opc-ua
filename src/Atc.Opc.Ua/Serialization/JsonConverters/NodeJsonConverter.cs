namespace Atc.Opc.Ua.Serialization.JsonConverters;

/// <summary>
/// JSON converter that handles (de)serialization of <see cref="NodeBase"/> polymorphic graph
/// (<see cref="NodeObject"/> / <see cref="NodeVariable"/>) plus a <see cref="NodeScanResult"/> wrapper.
/// Adds a simple discriminator property "$type": "object" | "variable".
/// </summary>
public sealed class NodeJsonConverter : JsonConverter<NodeBase>
{
    private const string Discriminator = "$type";
    private const string ObjectDiscriminatorValue = "object";
    private const string VariableDiscriminatorValue = "variable";

    public override bool CanConvert(Type typeToConvert)
        => typeToConvert == typeof(NodeBase);

    public override NodeBase? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of object for NodeBase.");
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty(Discriminator, out var discProp))
        {
            throw new JsonException($"Missing discriminator '{Discriminator}' for NodeBase");
        }

        var disc = discProp.GetString();
        NodeBase instance = disc switch
        {
            ObjectDiscriminatorValue => new NodeObject { NodeClass = NodeClassType.Object },
            VariableDiscriminatorValue => new NodeVariable { NodeClass = NodeClassType.Variable },
            _ => throw new JsonException($"Unknown node discriminator '{disc}'"),
        };

        var namingHelper = NodeJsonConverterNameHelper.For(options.PropertyNamingPolicy);

        PopulateScalars(root, instance, namingHelper);

        switch (instance)
        {
            case NodeVariable nodeVariable:
                PopulateVariableSpecific(root, nodeVariable, namingHelper);
                break;
            case NodeObject nodeObject:
            {
                if (TryGetArray(root, namingHelper.NodeObjects, out var arr))
                {
                    foreach (var child in arr.EnumerateArray())
                    {
                        if (child.Deserialize<NodeBase>(options) is NodeObject childObj)
                        {
                            nodeObject.NodeObjects.Add(childObj);
                        }
                    }
                }

                break;
            }
        }

        if (TryGetArray(root, namingHelper.NodeVariables, out var vars))
        {
            foreach (var child in vars.EnumerateArray())
            {
                if (child.Deserialize<NodeBase>(options) is NodeVariable childVar)
                {
                    instance.NodeVariables.Add(childVar);
                }
            }
        }

        return instance;
    }

    public override void Write(
        Utf8JsonWriter writer,
        NodeBase value,
        JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(options);

        var namingHelper = NodeJsonConverterNameHelper.For(options.PropertyNamingPolicy);

        writer.WriteStartObject();
        writer.WriteString(
            Discriminator,
            value.NodeClass == NodeClassType.Object ? ObjectDiscriminatorValue : VariableDiscriminatorValue);

        // Common
        writer.WriteString(namingHelper.ParentNodeId, value.ParentNodeId);
        writer.WriteString(namingHelper.NodeId, value.NodeId);
        writer.WriteString(namingHelper.NodeIdentifier, value.NodeIdentifier);
        writer.WriteString(namingHelper.NodeClass, value.NodeClass.ToString());
        writer.WriteString(namingHelper.DisplayName, value.DisplayName);

        switch (value)
        {
            case NodeObject obj:
                writer.WritePropertyName(namingHelper.NodeObjects);
                writer.WriteStartArray();
                foreach (var childObj in obj.NodeObjects)
                {
                    JsonSerializer.Serialize<NodeBase>(writer, childObj, options);
                }

                writer.WriteEndArray();

                writer.WritePropertyName(namingHelper.NodeVariables);
                writer.WriteStartArray();
                foreach (var v in obj.NodeVariables)
                {
                    JsonSerializer.Serialize<NodeBase>(writer, v, options);
                }

                writer.WriteEndArray();
                break;

            case NodeVariable v:
                WriteDotNetTypeDescriptor(writer, namingHelper.DataTypeDotnet, v.DataTypeDotnet, namingHelper);
                writer.WriteString(namingHelper.SampleValue, v.SampleValue);

                if (v.DataTypeOpcUa is not null)
                {
                    writer.WritePropertyName(namingHelper.DataTypeOpcUa);
                    WriteOpUaDataType(writer, v.DataTypeOpcUa, namingHelper);
                }

                writer.WritePropertyName(namingHelper.NodeVariables);
                writer.WriteStartArray();
                foreach (var childVar in v.NodeVariables)
                {
                    JsonSerializer.Serialize<NodeBase>(writer, childVar, options);
                }

                writer.WriteEndArray();
                break;
        }

        writer.WriteEndObject();
    }

    private static void PopulateScalars(
        JsonElement rootElement,
        NodeBase instance,
        NodeJsonConverterNameHelper namingHelper)
    {
        if (TryGetString(rootElement, namingHelper.ParentNodeId, out var s))
        {
            instance.ParentNodeId = s!;
        }

        if (TryGetString(rootElement, namingHelper.NodeId, out s))
        {
            instance.NodeId = s!;
        }

        if (TryGetString(rootElement, namingHelper.NodeIdentifier, out s))
        {
            instance.NodeIdentifier = s!;
        }

        if (TryGetString(rootElement, namingHelper.DisplayName, out s))
        {
            instance.DisplayName = s!;
        }
    }

    private static void PopulateVariableSpecific(
        JsonElement rootElement,
        NodeVariable nodeVariable,
        NodeJsonConverterNameHelper namingHelper)
    {
        if (rootElement.TryGetProperty(namingHelper.DataTypeDotnet, out var dotNetEl) &&
            dotNetEl.ValueKind == JsonValueKind.Object)
        {
            nodeVariable.DataTypeDotnet = ReadDotNetTypeDescriptor(dotNetEl, namingHelper);
        }

        if (TryGetString(rootElement, namingHelper.SampleValue, out var s))
        {
            nodeVariable.SampleValue = s!;
        }

        if (rootElement.TryGetProperty(namingHelper.DataTypeOpcUa, out var dt) &&
            dt.ValueKind == JsonValueKind.Object)
        {
            nodeVariable.DataTypeOpcUa = ReadOpUaDataType(dt, namingHelper);
        }
    }

    private static bool TryGetString(
        JsonElement jsonElement,
        string name,
        out string? value)
    {
        if (jsonElement.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String)
        {
            value = p.GetString();
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryGetArray(
        JsonElement jsonElement,
        string name,
        out JsonElement value)
        => jsonElement.TryGetProperty(name, out value) &&
           value.ValueKind == JsonValueKind.Array;

    private static void WriteOpUaDataType(
        Utf8JsonWriter writer,
        OpUaDataType dataType,
        NodeJsonConverterNameHelper namingHelper)
    {
        writer.WriteStartObject();
        writer.WriteString(namingHelper.OpUaNodeId, dataType.NodeId);
        writer.WriteString(namingHelper.OpUaName, dataType.Name);
        writer.WriteString(namingHelper.OpUaDisplayName, dataType.DisplayName);
        writer.WriteString(namingHelper.OpUaIdentifierType, dataType.IdentifierType);
        writer.WriteString(namingHelper.OpUaKind, dataType.Kind.ToString());
        writer.WriteBoolean(namingHelper.OpUaIsArray, dataType.IsArray);
        writer.WriteEndObject();
    }

    private static void WriteDotNetTypeDescriptor(
        Utf8JsonWriter writer,
        string propertyName,
        DotNetTypeDescriptor? descriptor,
        NodeJsonConverterNameHelper namingHelper)
    {
        if (descriptor is null)
        {
            writer.WriteNull(propertyName);
            return;
        }

        writer.WritePropertyName(propertyName);
        WriteDotNetTypeDescriptorValue(writer, descriptor, namingHelper);
    }

    private static void WriteDotNetTypeDescriptorValue(
        Utf8JsonWriter writer,
        DotNetTypeDescriptor descriptor,
        NodeJsonConverterNameHelper namingHelper)
    {
        writer.WriteStartObject();
        writer.WriteString(namingHelper.DotNetKind, descriptor.Kind.ToString());
        writer.WriteString(namingHelper.DotNetName, descriptor.Name);
        writer.WriteString(namingHelper.DotNetClrTypeName, descriptor.ClrTypeName);

        if (descriptor.ArrayElementType is not null)
        {
            writer.WritePropertyName(namingHelper.DotNetArrayElementType);
            WriteDotNetTypeDescriptorValue(writer, descriptor.ArrayElementType, namingHelper);
        }

        if (descriptor.EnumMembers is { Count: > 0 })
        {
            writer.WritePropertyName(namingHelper.DotNetEnumMembers);
            writer.WriteStartArray();
            foreach (var member in descriptor.EnumMembers)
            {
                writer.WriteStartObject();
                writer.WriteNumber(namingHelper.EnumMemberValue, member.Value);
                writer.WriteString(namingHelper.EnumMemberName, member.Name);
                if (member.DisplayName is not null)
                {
                    writer.WriteString(namingHelper.EnumMemberDisplayName, member.DisplayName);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        if (descriptor.StructureFields is { Count: > 0 })
        {
            writer.WritePropertyName(namingHelper.DotNetStructureFields);
            writer.WriteStartArray();
            foreach (var field in descriptor.StructureFields)
            {
                writer.WriteStartObject();
                writer.WriteString(namingHelper.FieldName, field.Name);
                writer.WriteBoolean(namingHelper.FieldIsOptional, field.IsOptional);
                if (field.Type is not null)
                {
                    writer.WritePropertyName(namingHelper.FieldType);
                    WriteDotNetTypeDescriptorValue(writer, field.Type, namingHelper);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static DotNetTypeDescriptor? ReadDotNetTypeDescriptor(
        JsonElement element,
        NodeJsonConverterNameHelper namingHelper)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var descriptor = new DotNetTypeDescriptor();

        if (TryGetString(element, namingHelper.DotNetKind, out var kindStr) &&
            Enum.TryParse<DotNetTypeKind>(kindStr, ignoreCase: true, out var kind))
        {
            descriptor.Kind = kind;
        }

        if (TryGetString(element, namingHelper.DotNetName, out var name))
        {
            descriptor.Name = name ?? string.Empty;
        }

        if (TryGetString(element, namingHelper.DotNetClrTypeName, out var clrTypeName))
        {
            descriptor.ClrTypeName = clrTypeName ?? string.Empty;
        }

        if (element.TryGetProperty(namingHelper.DotNetArrayElementType, out var elementTypeEl) &&
            elementTypeEl.ValueKind == JsonValueKind.Object)
        {
            descriptor.ArrayElementType = ReadDotNetTypeDescriptor(elementTypeEl, namingHelper);
        }

        if (TryGetArray(element, namingHelper.DotNetEnumMembers, out var enumMembersEl))
        {
            var members = new List<DotNetEnumMember>();
            foreach (var memberEl in enumMembersEl.EnumerateArray())
            {
                var member = new DotNetEnumMember();
                if (memberEl.TryGetProperty(namingHelper.EnumMemberValue, out var valueEl) &&
                    valueEl.TryGetInt32(out var memberValue))
                {
                    member.Value = memberValue;
                }

                if (TryGetString(memberEl, namingHelper.EnumMemberName, out var memberName))
                {
                    member.Name = memberName!;
                }

                if (TryGetString(memberEl, namingHelper.EnumMemberDisplayName, out var memberDisplayName))
                {
                    member.DisplayName = memberDisplayName;
                }

                members.Add(member);
            }

            descriptor.EnumMembers = members;
        }

        if (TryGetArray(element, namingHelper.DotNetStructureFields, out var fieldsEl))
        {
            var fields = new List<DotNetFieldDescriptor>();
            foreach (var fieldEl in fieldsEl.EnumerateArray())
            {
                var field = new DotNetFieldDescriptor();
                if (TryGetString(fieldEl, namingHelper.FieldName, out var fieldName))
                {
                    field.Name = fieldName!;
                }

                if (fieldEl.TryGetProperty(namingHelper.FieldIsOptional, out var isOptionalEl) &&
                    isOptionalEl.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    field.IsOptional = isOptionalEl.GetBoolean();
                }

                if (fieldEl.TryGetProperty(namingHelper.FieldType, out var fieldTypeEl) &&
                    fieldTypeEl.ValueKind == JsonValueKind.Object)
                {
                    field.Type = ReadDotNetTypeDescriptor(fieldTypeEl, namingHelper);
                }

                fields.Add(field);
            }

            descriptor.StructureFields = fields;
        }

        return descriptor;
    }

    private static OpUaDataType ReadOpUaDataType(
        JsonElement element,
        NodeJsonConverterNameHelper namingHelper)
    {
        var dataType = new OpUaDataType();

        if (TryGetString(element, namingHelper.OpUaNodeId, out var nodeId))
        {
            dataType.NodeId = nodeId!;
        }

        if (TryGetString(element, namingHelper.OpUaName, out var name))
        {
            dataType.Name = name!;
        }

        if (TryGetString(element, namingHelper.OpUaDisplayName, out var displayName))
        {
            dataType.DisplayName = displayName!;
        }

        if (TryGetString(element, namingHelper.OpUaIdentifierType, out var identifierType))
        {
            dataType.IdentifierType = identifierType!;
        }

        if (TryGetString(element, namingHelper.OpUaKind, out var kindStr) &&
            Enum.TryParse<OpcUaTypeKind>(kindStr, ignoreCase: true, out var kind))
        {
            dataType.Kind = kind;
        }

        if (element.TryGetProperty(namingHelper.OpUaIsArray, out var isArr) &&
            isArr.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            dataType.IsArray = isArr.GetBoolean();
        }

        return dataType;
    }
}
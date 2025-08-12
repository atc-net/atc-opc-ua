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
            throw new JsonException($"Missing discriminator '{Discriminator}' for NodeBase.");
        }

        var disc = discProp.GetString();
        NodeBase instance = disc switch
        {
            ObjectDiscriminatorValue => new NodeObject { NodeClass = NodeClassType.Object },
            VariableDiscriminatorValue => new NodeVariable { NodeClass = NodeClassType.Variable },
            _ => throw new JsonException($"Unknown node discriminator '{disc}'."),
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
                writer.WriteString(namingHelper.DataTypeDotnet, v.DataTypeDotnet);
                writer.WriteString(namingHelper.SampleValue, v.SampleValue);

                if (v.DataTypeOpcUa is not null)
                {
                    writer.WritePropertyName(namingHelper.DataTypeOpcUa);
                    writer.WriteStartObject();
                    writer.WriteString(namingHelper.OpUaName, v.DataTypeOpcUa.Name);
                    writer.WriteBoolean(namingHelper.OpUaIsArray, v.DataTypeOpcUa.IsArray);
                    writer.WriteEndObject();
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
        if (TryGetString(rootElement, namingHelper.DataTypeDotnet, out var s))
        {
            nodeVariable.DataTypeDotnet = s!;
        }

        if (TryGetString(rootElement, namingHelper.SampleValue, out s))
        {
            nodeVariable.SampleValue = s!;
        }

        if (rootElement.TryGetProperty(namingHelper.DataTypeOpcUa, out var dt) &&
            dt.ValueKind == JsonValueKind.Object)
        {
            var m = new OpUaDataType();
            if (TryGetString(dt, namingHelper.OpUaName, out var name))
            {
                m.Name = name!;
            }

            if (dt.TryGetProperty(namingHelper.OpUaIsArray, out var isArr) &&
                isArr.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                m.IsArray = isArr.GetBoolean();
            }

            nodeVariable.DataTypeOpcUa = m;
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
}

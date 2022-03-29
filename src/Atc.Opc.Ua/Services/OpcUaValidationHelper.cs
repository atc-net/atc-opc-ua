namespace Atc.Opc.Ua.Services;

public static class OpcUaValidationHelper
{
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK")]
    public static string? GetErrorForArgumentData(
        string parameterNameDataTypes,
        string parameterNameDataValues,
        string[] parameterValueDataTypes,
        string[] parameterValueDataValues)
    {
        ArgumentNullException.ThrowIfNull(parameterValueDataTypes);
        ArgumentNullException.ThrowIfNull(parameterValueDataValues);

        if (string.IsNullOrEmpty(parameterNameDataTypes))
        {
            return $"--{parameterNameDataTypes} is not set.";
        }

        if (string.IsNullOrEmpty(parameterNameDataValues))
        {
            return $"--{parameterNameDataValues} is not set.";
        }

        if (parameterValueDataTypes.Length != parameterValueDataValues.Length)
        {
            return $"--{parameterNameDataTypes} and --{parameterNameDataValues} lists differs in length.";
        }

        for (var i = 0; i < parameterValueDataTypes.Length; i++)
        {
            if (Enum<OpcUaDataEncodingType>.TryParse(parameterValueDataTypes[i], out var dataType))
            {
                var dataValue = parameterValueDataValues[i];
                var isValid = false;
                switch (dataType)
                {
                    case OpcUaDataEncodingType.Boolean:
                        if (bool.TryParse(dataValue, out _))
                        {
                            isValid = true;
                        }

                        break;
                    case OpcUaDataEncodingType.SByte:
                        break;
                    case OpcUaDataEncodingType.Byte:
                        break;
                    case OpcUaDataEncodingType.Int16:
                        break;
                    case OpcUaDataEncodingType.UInt16:
                        break;
                    case OpcUaDataEncodingType.Int32:
                        if (int.TryParse(dataValue, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out _))
                        {
                            isValid = true;
                        }

                        break;
                    case OpcUaDataEncodingType.UInt32:
                        break;
                    case OpcUaDataEncodingType.Int64:
                        break;
                    case OpcUaDataEncodingType.UInt64:
                        break;
                    case OpcUaDataEncodingType.Float:
                        break;
                    case OpcUaDataEncodingType.Double:
                        if (double.TryParse(dataValue, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out _))
                        {
                            isValid = true;
                        }

                        break;
                    case OpcUaDataEncodingType.String:
                        break;
                    case OpcUaDataEncodingType.DateTime:
                        break;
                    case OpcUaDataEncodingType.Guid:
                        break;
                    case OpcUaDataEncodingType.ByteString:
                        break;
                    case OpcUaDataEncodingType.XmlElement:
                        break;
                    case OpcUaDataEncodingType.NodeId:
                        break;
                    case OpcUaDataEncodingType.ExpandedNodeId:
                        break;
                    case OpcUaDataEncodingType.StatusCode:
                        break;
                    case OpcUaDataEncodingType.QualifiedName:
                        break;
                    case OpcUaDataEncodingType.LocalizedText:
                        break;
                    case OpcUaDataEncodingType.ExtensionObject:
                        break;
                    case OpcUaDataEncodingType.DataValue:
                        break;
                    case OpcUaDataEncodingType.Variant:
                        break;
                    case OpcUaDataEncodingType.DiagnosticInfo:
                        break;
                    default:
                        return $"--{parameterNameDataTypes} contains a unsupported data type: {parameterValueDataTypes[i]}";
                }

                if (!isValid)
                {
                    return $"--{parameterNameDataTypes} contains a invalid value: '{dataValue}' for data type '{dataType}'";
                }
            }
            else
            {
                return $"--{parameterNameDataTypes} contains a unsupported data type: {parameterValueDataTypes[i]}";
            }
        }

        return null;
    }
}
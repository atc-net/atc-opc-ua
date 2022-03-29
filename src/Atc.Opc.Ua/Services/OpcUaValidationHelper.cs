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
            if (!Enum<OpcUaDataEncodingType>.TryParse(parameterValueDataTypes[i], out var dataType))
            {
                return $"--{parameterNameDataTypes} contains an unsupported data type: {parameterValueDataTypes[i]}";
            }

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
                    if (sbyte.TryParse(dataValue, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out _))
                    {
                        isValid = true;
                    }

                    break;
                case OpcUaDataEncodingType.Byte:
                    if (byte.TryParse(dataValue, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out _))
                    {
                        isValid = true;
                    }

                    break;
                case OpcUaDataEncodingType.Int16:
                    if (short.TryParse(dataValue, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out _))
                    {
                        isValid = true;
                    }

                    break;
                case OpcUaDataEncodingType.UInt16:
                    if (ushort.TryParse(dataValue, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out _))
                    {
                        isValid = true;
                    }

                    break;
                case OpcUaDataEncodingType.Int32:
                    if (int.TryParse(dataValue, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out _))
                    {
                        isValid = true;
                    }

                    break;
                case OpcUaDataEncodingType.UInt32:
                    if (uint.TryParse(dataValue, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out _))
                    {
                        isValid = true;
                    }

                    break;
                case OpcUaDataEncodingType.Int64:
                    if (long.TryParse(dataValue, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out _))
                    {
                        isValid = true;
                    }

                    break;
                case OpcUaDataEncodingType.UInt64:
                    if (ulong.TryParse(dataValue, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out _))
                    {
                        isValid = true;
                    }

                    break;
                case OpcUaDataEncodingType.Float:
                    if (float.TryParse(dataValue, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out _))
                    {
                        isValid = true;
                    }

                    break;
                case OpcUaDataEncodingType.Double:
                    if (double.TryParse(dataValue, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out _))
                    {
                        isValid = true;
                    }

                    break;
                case OpcUaDataEncodingType.String:
                    isValid = true;

                    break;
                case OpcUaDataEncodingType.DateTime:
                    if (DateTime.TryParse(dataValue, GlobalizationConstants.EnglishCultureInfo, DateTimeStyles.None, out _))
                    {
                        isValid = true;
                    }

                    break;
                case OpcUaDataEncodingType.Guid:
                    if (Guid.TryParse(dataValue, out _))
                    {
                        isValid = true;
                    }

                    break;
                case OpcUaDataEncodingType.ByteString:
                case OpcUaDataEncodingType.XmlElement:
                case OpcUaDataEncodingType.NodeId:
                case OpcUaDataEncodingType.ExpandedNodeId:
                case OpcUaDataEncodingType.StatusCode:
                case OpcUaDataEncodingType.QualifiedName:
                case OpcUaDataEncodingType.LocalizedText:
                case OpcUaDataEncodingType.ExtensionObject:
                case OpcUaDataEncodingType.DataValue:
                case OpcUaDataEncodingType.Variant:
                case OpcUaDataEncodingType.DiagnosticInfo:
                    return
                        $"--{parameterNameDataTypes} contains data type '{parameterValueDataTypes[i]}', which is not supported by this library.";
                default:
                    return
                        $"--{parameterNameDataTypes} contains an unsupported data type: {parameterValueDataTypes[i]}";
            }

            if (!isValid)
            {
                return
                    $"--{parameterNameDataValues} contains an invalid value: '{dataValue}' for data type '{dataType}'";
            }
        }

        return null;
    }
}
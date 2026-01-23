namespace Atc.Opc.Ua.Contracts;

public record MethodExecutionParameter(
    OpcUaDataEncodingType DataEncoding,
    string Value);
namespace Atc.Opc.Ua.Contracts;

public record MethodExecutionResult(
    OpcUaDataEncodingType DataEncoding,
    string Value);
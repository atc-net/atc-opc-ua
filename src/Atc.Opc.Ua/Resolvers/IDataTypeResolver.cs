namespace Atc.Opc.Ua.Resolvers;

/// <summary>
/// Defines a mechanism for resolving a .NET type from its name representation.
/// </summary>
public interface IDataTypeResolver
{
    /// <summary>
    /// Gets the corresponding .NET type for a given type name.
    /// </summary>
    /// <param name="value">The name of the type to resolve.</param>
    /// <returns>The corresponding .NET Type.</returns>
    Type GetTypeByName(
        string? value);
}
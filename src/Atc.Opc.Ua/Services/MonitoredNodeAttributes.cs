namespace Atc.Opc.Ua.Services;

/// <summary>
/// The attributes the client reads for a node before monitoring it: whether it exists, its display name, its data type
/// and its access level.
/// </summary>
/// <param name="StatusCode">The status of reading the node's display name; Bad, e.g. <c>BadNodeIdUnknown</c>, when the node can't be monitored.</param>
/// <param name="DisplayName">The node's display name, or <see langword="null"/> if it couldn't be read.</param>
/// <param name="DataTypeId">The node's data type, or <see langword="null"/> if it couldn't be read.</param>
/// <param name="AccessLevel">The node's access level, or <see langword="null"/> if it couldn't be read.</param>
internal sealed record MonitoredNodeAttributes(
    uint StatusCode,
    string? DisplayName,
    NodeId? DataTypeId,
    byte? AccessLevel)
{
    /// <summary>
    /// The attributes read per node, in this order.
    /// </summary>
    public static readonly uint[] AttributeIds = [Attributes.DisplayName, Attributes.DataType, Attributes.AccessLevel];

    /// <summary>
    /// Creates the attributes from the values read for one node, in the order of <see cref="AttributeIds"/>.
    /// </summary>
    /// <param name="displayName">The DisplayName attribute.</param>
    /// <param name="dataType">The DataType attribute.</param>
    /// <param name="accessLevel">The AccessLevel attribute.</param>
    /// <returns>The attributes.</returns>
    public static MonitoredNodeAttributes FromDataValues(
        DataValue displayName,
        DataValue dataType,
        DataValue accessLevel)
    {
        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentNullException.ThrowIfNull(dataType);
        ArgumentNullException.ThrowIfNull(accessLevel);

        return new MonitoredNodeAttributes(
            displayName.StatusCode.Code,
            IsGood(displayName) ? (displayName.Value as LocalizedText)?.Text : null,
            IsGood(dataType) ? dataType.Value as NodeId : null,
            IsGood(accessLevel) && accessLevel.Value is byte level ? level : null);
    }

    private static bool IsGood(DataValue value)
        => global::Opc.Ua.StatusCode.IsGood(value.StatusCode);
}
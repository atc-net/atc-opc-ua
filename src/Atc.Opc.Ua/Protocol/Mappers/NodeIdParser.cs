namespace Atc.Opc.Ua.Protocol.Mappers;

/// <summary>
/// Reads the node ids callers give as text, e.g. <c>ns=3;s=Motor</c>, the same way in every call.
/// </summary>
internal static class NodeIdParser
{
    /// <summary>
    /// Reads a node id.
    /// </summary>
    /// <param name="nodeId">The node id as text.</param>
    /// <param name="paramName">The caller's parameter, for the exception.</param>
    /// <returns>The node id.</returns>
    /// <exception cref="ArgumentException"><paramref name="nodeId"/> isn't a valid node id.</exception>
    public static NodeId Parse(
        string nodeId,
        string paramName)
        => TryParse(nodeId, out var parsed)
            ? parsed
            : throw new ArgumentException($"'{nodeId}' isn't a valid node id.", paramName);

    /// <summary>
    /// Reads a node id without throwing.
    /// </summary>
    /// <param name="nodeId">The node id as text.</param>
    /// <param name="parsed">The node id, or <see cref="NodeId.Null"/> when it isn't valid.</param>
    /// <returns><see langword="true"/> when it's valid.</returns>
    public static bool TryParse(
        string nodeId,
        out NodeId parsed)
    {
        try
        {
            parsed = NodeId.Parse(nodeId);
            return true;
        }
        catch (Exception ex) when (ex is ServiceResultException or ArgumentException or FormatException)
        {
            parsed = NodeId.Null;
            return false;
        }
    }
}
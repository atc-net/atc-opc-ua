namespace Atc.Opc.Ua.Protocol.Runtime.Extensions;

/// <summary>
/// Transport quota config extensions.
/// </summary>
public static class TransportQuotaConfigExtensions
{
    /// <summary>
    /// Default values for transport quotas.
    /// </summary>
    public const int DefaultSecurityTokenLifetime = 60 * 60 * 1000;
    public const int DefaultChannelLifetime = 300 * 1000;
    public const int DefaultMaxBufferSize = (64 * 1024) - 1;
    public const int DefaultMaxMessageSize = 4 * 1024 * 1024;
    public const int DefaultMaxArrayLength = (64 * 1024) - 1;
    public const int DefaultMaxByteStringLength = 1024 * 1024;
    public const int DefaultMaxStringLength = (128 * 1024) - 256;
    public const int DefaultOperationTimeout = 120 * 1000;

    /// <summary>
    /// Return service defaults for the TransportQuotas.
    /// </summary>
    /// <returns>Generated TransportQuotas.</returns>
    public static TransportQuotas DefaultTransportQuotas()
        => new()
        {
            MaxMessageSize = DefaultMaxMessageSize,
            OperationTimeout = DefaultOperationTimeout,
            MaxStringLength = DefaultMaxStringLength,
            MaxByteStringLength = DefaultMaxByteStringLength,
            MaxArrayLength = DefaultMaxArrayLength,
            MaxBufferSize = DefaultMaxBufferSize,
            ChannelLifetime = DefaultChannelLifetime,
            SecurityTokenLifetime = DefaultSecurityTokenLifetime,
        };

    /// <summary>
    /// Convert to transport quota.
    /// </summary>
    /// <param name="transportQuotaConfig">The Transport Quota Config.</param>
    /// <returns>Generated TransportQuotas.</returns>
    public static TransportQuotas ToTransportQuotas(
        this ITransportQuotaConfig transportQuotaConfig)
    {
        ArgumentNullException.ThrowIfNull(transportQuotaConfig);

        return new TransportQuotas
        {
            OperationTimeout = transportQuotaConfig.OperationTimeout,
            MaxStringLength = transportQuotaConfig.MaxStringLength,
            MaxByteStringLength = transportQuotaConfig.MaxByteStringLength,
            MaxArrayLength = transportQuotaConfig.MaxArrayLength,
            MaxMessageSize = transportQuotaConfig.MaxMessageSize,
            MaxBufferSize = transportQuotaConfig.MaxBufferSize,
            ChannelLifetime = transportQuotaConfig.ChannelLifetime,
            SecurityTokenLifetime = transportQuotaConfig.SecurityTokenLifetime,
        };
    }

    /// <summary>
    /// Convert to endpoint configuration.
    /// </summary>
    /// <param name="transportQuotaConfig">The Transport Quota Config.</param>
    /// <returns>Generated EndpointConfiguration.</returns>
    public static EndpointConfiguration ToEndpointConfiguration(
        this ITransportQuotaConfig transportQuotaConfig)
    {
        ArgumentNullException.ThrowIfNull(transportQuotaConfig);

        return new EndpointConfiguration
        {
            OperationTimeout = transportQuotaConfig.OperationTimeout,
            UseBinaryEncoding = true,
            MaxArrayLength = transportQuotaConfig.MaxArrayLength,
            MaxByteStringLength = transportQuotaConfig.MaxByteStringLength,
            MaxMessageSize = transportQuotaConfig.MaxMessageSize,
            MaxStringLength = transportQuotaConfig.MaxStringLength,
            MaxBufferSize = transportQuotaConfig.MaxBufferSize,
            ChannelLifetime = transportQuotaConfig.ChannelLifetime,
            SecurityTokenLifetime = transportQuotaConfig.SecurityTokenLifetime,
        };
    }
}
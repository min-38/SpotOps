namespace SpotOps.Infrastructure.Redis;

public sealed class RedisKeyBuilder
{
    private readonly string _prefix;

    public RedisKeyBuilder(string keyPrefix)
    {
        _prefix = string.IsNullOrWhiteSpace(keyPrefix) ? "spotops" : keyPrefix.Trim();
    }

    public string QueueEvent(Guid eventId) => $"{_prefix}:queue:event:{eventId}";
}

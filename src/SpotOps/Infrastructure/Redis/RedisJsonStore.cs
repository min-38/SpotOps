using System.Text.Json;
using StackExchange.Redis;

namespace SpotOps.Infrastructure.Redis;

public sealed class RedisJsonStore<T>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDatabase _redis;
    private readonly int _defaultTtlHours;

    public RedisJsonStore(IDatabase redis, int defaultTtlHours)
    {
        _redis = redis;
        _defaultTtlHours = defaultTtlHours;
    }

    public async Task<T?> GetAsync(string key)
    {
        var raw = await _redis.StringGetAsync(key);
        if (raw.IsNullOrEmpty) return default;
        return JsonSerializer.Deserialize<T>(raw.ToString(), JsonOptions);
    }

    public Task SetAsync(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        return _redis.StringSetAsync(key, json, TimeSpan.FromHours(_defaultTtlHours));
    }
}
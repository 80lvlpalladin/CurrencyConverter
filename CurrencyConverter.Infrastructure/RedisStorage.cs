using System.Text.Json;
using StackExchange.Redis;

namespace CurrencyConverter.Infrastructure;

public class RedisStorage(string connectionString)
{
    private readonly ConnectionMultiplexer _connectionMultiplexer =
        ConnectionMultiplexer.Connect(connectionString, options =>{ options.AllowAdmin = true; });

    internal async Task SetAsync<T>(string key, T value, TimeSpan? expiry)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var jsonValueString = JsonSerializer.Serialize(value);
        await database.StringSetAsync(key, jsonValueString, expiry);
    }

    internal async Task ClearAsync()
    {
        foreach (var endpoint in _connectionMultiplexer.GetEndPoints())
        {
            var server = _connectionMultiplexer.GetServer(endpoint);
            await server.FlushAllDatabasesAsync();
        }
    }

    internal async Task RemoveBatchAsync(string keyPrefix, CancellationToken cancellationToken)
    {
        var database = _connectionMultiplexer.GetDatabase();
        foreach (var endpoint in _connectionMultiplexer.GetEndPoints())
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var server = _connectionMultiplexer.GetServer(endpoint);

            await foreach (var key in server
                               .KeysAsync(pattern: $"{keyPrefix}*")
                               .WithCancellation(cancellationToken))
            {
                await database.KeyDeleteAsync(key);
            }
        }
    }
    
    internal async Task<bool> IsEmptyAsync()
    {
        var database = _connectionMultiplexer.GetDatabase();
        var dbsize = await database.ExecuteAsync("DBSIZE");
        return dbsize.ToString() == "0";
    }

    internal async Task<T?> GetAsync<T>(string key) where T : class
    {
        var database = _connectionMultiplexer.GetDatabase();
        var redisValue = await database.StringGetAsync(key);

        if (string.IsNullOrWhiteSpace(redisValue))
            return null;

        if (typeof(T) == typeof(string))
        {
            return (T)(object) redisValue.ToString();
        }

        return JsonSerializer.Deserialize<T>(redisValue.ToString());
    }


    internal async Task<IReadOnlyCollection<string>> ListAllKeysAsync
        (string keyPattern = "*", CancellationToken cancellationToken = default)
    {
        List<string> result = [];

        foreach (var endpoint in _connectionMultiplexer.GetEndPoints())
        {
            if(cancellationToken.IsCancellationRequested)
                break;

            var server = _connectionMultiplexer.GetServer(endpoint);

            await foreach (var key in
                           server.KeysAsync(pattern: keyPattern).WithCancellation(cancellationToken))
            {
                result.Add(key.ToString());
            }
        }

        return result;
    }
}
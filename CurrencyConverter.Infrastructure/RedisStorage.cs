using System.Text.Json;
using CurrencyConverter.Domain;
using StackExchange.Redis;

namespace CurrencyConverter.Infrastructure;

public class RedisStorage(string connectionString)
{
    private readonly ConnectionMultiplexer _connectionMultiplexer =
        ConnectionMultiplexer.Connect(connectionString, options =>{ options.AllowAdmin = true; });

    internal Task<bool> SaveAsync<T>(string key, T value, TimeSpan? expiry)
    {
        var database = _connectionMultiplexer.GetDatabase();
        var jsonValueString = JsonSerializer.Serialize(value);
        return database.StringSetAsync(key, jsonValueString, expiry);
    }

    /// <summary>
    /// Saves a collection of values into Redis, optionally paginating the data, and sets an expiry time for the stored data.
    /// </summary>
    /// <typeparam name="T">The type of objects in the collection to be saved.</typeparam>
    /// <param name="key">The unique identifier for the Redis hash where the values will be stored.</param>
    /// <param name="values">The collection of values to be saved. Cannot be an empty collection.</param>
    /// <param name="paginationOptions">Optional. Pagination options specifying the maximum number of items per page. If null, all values will be stored under one key.</param>
    /// <param name="expiry">Optional. The expiration time after which the stored data will be removed from Redis. If null, the data will not expire.</param>
    /// <returns>The total number of pages created during pagination.</returns>
    /// <exception cref="ArgumentException">Thrown when the key is null, empty, or whitespace, or if the values collection is empty.</exception>
    internal async Task<int> SaveManyAsync<T>(
        string key,
        IReadOnlyCollection<T> values,
        PaginationOptions? paginationOptions = null,
        TimeSpan? expiry = null)
    {
        if (values.Count == 0)
            throw new ArgumentException("Value cannot be an empty collection.", nameof(values));

        ArgumentException.ThrowIfNullOrWhiteSpace(nameof(key));

        var hashKey = key + 
                      (paginationOptions is null ? "_all" : $"_{paginationOptions.MaxPageSize}");

        var maxPageSize = paginationOptions?.MaxPageSize ?? values.Count;
        
        var hashEntries = PaginationHelper
            .SplitIntoPages(values, maxPageSize)
            .Select(valuesPerPage => 
                new HashEntry(
                    valuesPerPage.pageNumber.ToString(), 
                    JsonSerializer.Serialize(valuesPerPage.values)))
            .ToArray();

        var database = _connectionMultiplexer.GetDatabase();
        
        await database.HashSetAsync(hashKey, hashEntries);
        await database.KeyExpireAsync(hashKey, expiry);

        return hashEntries.Length;
    }

    internal async Task<(PaginationInfo paginationInfo, T[] values)?> GetManyAsync<T>
        (string key, PaginationOptions? paginationOptions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nameof(key));

        var hashKey = key + 
                      (paginationOptions is null ? "_all" : $"_{paginationOptions.MaxPageSize}");
        
        var hashField = $"{paginationOptions?.PageNumber ?? 1}";
        
        var database = _connectionMultiplexer.GetDatabase();
        
        var serializedValues = await database.HashGetAsync(hashKey, hashField);
        
        if (serializedValues.IsNullOrEmpty)
            return null;
        
        var deserializedValues = JsonSerializer.Deserialize<T[]>(serializedValues!);

        if (deserializedValues is null)
            return null;
        
        var paginationInfo = new PaginationInfo(
            CurrentPageNumber: paginationOptions?.PageNumber ?? 1, 
            CurrentPageSize: (ushort) deserializedValues.Length, 
            PageCountTotal: (ushort) await database.HashLengthAsync(hashKey));
        
        return (paginationInfo, deserializedValues);
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
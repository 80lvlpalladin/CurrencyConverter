using CurrencyConverter.Domain;
using CurrencyConverter.Infrastructure.Clients;
using Microsoft.Extensions.Options;

namespace CurrencyConverter.Infrastructure.ExchangeRateProviders;

public class FrankfurterApiOptions
{
    public const string SectionName = "FrankfurterApi";
    public ushort HistoryCacheExpiryHours { get; set; } = 24;
    
    public required string BaseUrl { get; set; }
}

public class FrankfurterExchangeRateProvider : IExchangeRateProvider
{
    private readonly IFrankfurterApiClient _apiClient;
    private readonly RedisStorage _redisStorage;
    private readonly TimeSpan _historyCacheExpiry;

    // ReSharper disable once ConvertToPrimaryConstructor
    public FrankfurterExchangeRateProvider(
        IFrankfurterApiClient apiClient,
        RedisStorage redisStorage, 
        IOptions<FrankfurterApiOptions> options)
    {
        _apiClient = apiClient;
        _redisStorage = redisStorage;
        _historyCacheExpiry = TimeSpan.FromHours(options.Value.HistoryCacheExpiryHours);
    }

    public string Id => "frankfurter";
    
    public async Task<ExchangeRate> GetLatestAsync
        (string baseCurrency, CancellationToken cancellationToken = default)
    {
        //we do not try to get exchange rate from cache because there is no guarantee
        // there is no new exchange rate available in API
        var apiResponse = 
            await _apiClient.GetLatestExchangeRatesAsync(baseCurrency, cancellationToken);

        var cacheKey = CreateHistoryCacheKey(baseCurrency, apiResponse.Date);
        
        await _redisStorage.SetAsync(cacheKey, apiResponse.Rates, _historyCacheExpiry);
        
        return new ExchangeRate(apiResponse.Date, apiResponse.Rates);
    }

    public async Task<decimal> ConvertAsync(
        string baseCurrency, 
        decimal amount, 
        string targetCurrency, 
        CancellationToken cancellationToken = default)
    {
        //we do not try to get exchange rate from cache first for the same reason as above
        var apiResponse = await _apiClient.GetLatestExchangeRatesAsync
            (baseCurrency, targetCurrency, cancellationToken);
        
        if(!apiResponse.Rates.TryGetValue(targetCurrency.ToUpper(), out var rate))
            throw new InvalidOperationException
                ($"Exchange rate {targetCurrency} was absent from Frankfurter API response.");

        return amount * rate;
    }

    public async Task<ExchangeRateHistory> GetHistoryAsync(
        string baseCurrency, 
        string startDate, 
        string endDate, 
        CancellationToken cancellationToken = default)
    {
        var endDateTime = DateTime.Parse(endDate);
        var nonCachedDatesSegment = new List<string> ();
        var resultExchangeRates = new List<ExchangeRate>();
        
        for (var dateTime = DateTime.Parse(startDate);
            dateTime <= endDateTime; 
            dateTime = dateTime.AddDays(1))
        {
            var dateString = dateTime.ToString("yyyy-MM-dd");
            var cacheKey = CreateHistoryCacheKey(baseCurrency, dateString);

            var cachedExchangeRate = 
                await _redisStorage.GetAsync<Dictionary<string, decimal>>(cacheKey);

            if (cachedExchangeRate is not null && nonCachedDatesSegment.Count > 1)
            {
                var exchangeRatesFromApi = await GetExchangeRatesFromApi(
                    baseCurrency, 
                    nonCachedDatesSegment.First(), 
                    nonCachedDatesSegment.Last(), 
                    cancellationToken);
                
                resultExchangeRates.AddRange(exchangeRatesFromApi);
                
                resultExchangeRates.Add(new ExchangeRate(dateString, cachedExchangeRate));
                
                nonCachedDatesSegment.Clear();
            }
            else
            {
                nonCachedDatesSegment.Add(dateString);
            }

            if (dateTime == endDateTime && nonCachedDatesSegment.Count > 0)
            {
                var exchangeRatesFromApi = await GetExchangeRatesFromApi(
                    baseCurrency, 
                    nonCachedDatesSegment.First(), 
                    nonCachedDatesSegment.Last(), 
                    cancellationToken);
                
                resultExchangeRates.AddRange(exchangeRatesFromApi);
            }
        }
        
        return new ExchangeRateHistory(startDate, endDate, resultExchangeRates);

    }

    private async Task<IEnumerable<ExchangeRate>> GetExchangeRatesFromApi(
        string baseCurrency, 
        string startDate, 
        string endDate, 
        CancellationToken cancellationToken = default)
    {
        var apiResponse = await _apiClient.GetHistoricalRatesAsync
            (startDate, endDate, baseCurrency, cancellationToken);

        await SaveApiResponseToCacheAsync(apiResponse);

        return apiResponse.Rates
            .Select(rate => new ExchangeRate(rate.Key, rate.Value));
    }

    private ExchangeRateHistory ConvertToDomainModel(HistoricalRatesFrankfurterApiResponse apiResponse)
    {
        var exchangeRates = apiResponse.Rates
            .Select(rates => new ExchangeRate(rates.Key, rates.Value))
            .ToArray();

        return new ExchangeRateHistory(apiResponse.StartDate, apiResponse.EndDate, exchangeRates);
    }
    
    private Task SaveApiResponseToCacheAsync(HistoricalRatesFrankfurterApiResponse apiResponse)
    {
        return Task.WhenAll(apiResponse.Rates.Keys.Select(date =>
        {
            var cacheKey = CreateHistoryCacheKey(apiResponse.Base, date);
            return _redisStorage.SetAsync(cacheKey, apiResponse.Rates[date], _historyCacheExpiry);
        }));
    }
    
    private string CreateHistoryCacheKey(string baseCurrency, string dateString)
    {
        return Id + "_" + baseCurrency + "_" + dateString;
    }

    private (string id, string baseCurrency, string date) DeconstructHistoryCacheKey(string key)
    {
        var keySegments = key.Split("_");
        return (keySegments[0], keySegments[1], keySegments[2]);
    }

}
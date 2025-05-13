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
        //we do not cache this because we don't know when
        //the new exchange rate will be available in the API,
        //hence we don't know when to invalidate the cache
        var apiResponse = 
            await _apiClient.GetLatestExchangeRatesAsync(baseCurrency, cancellationToken);

        
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
        PaginationOptions? paginationOptions = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CreateHistoryCacheKey(baseCurrency, startDate, endDate);
        
        var cachedExchangeRates = 
            await _redisStorage.GetManyAsync<ExchangeRate>(cacheKey, paginationOptions);

        if (cachedExchangeRates is not null && cachedExchangeRates.Value.values.Length > 0)
        {
            return new ExchangeRateHistory(
                startDate, 
                endDate, 
                cachedExchangeRates.Value.values, 
                cachedExchangeRates.Value.paginationInfo);
        }
        
        var exchangeRatesFromApi = await GetExchangeRatesFromApi
            (baseCurrency, startDate, endDate, cancellationToken);
        
        var totalPageCount = await _redisStorage.SaveManyAsync
            (cacheKey, exchangeRatesFromApi, paginationOptions, _historyCacheExpiry);
        
        paginationOptions ??= 
            new PaginationOptions(1, (ushort) exchangeRatesFromApi.Count);
        
        var requestedPageValues = 
            PaginationHelper.GetPage(exchangeRatesFromApi, paginationOptions);
        
        var paginationInfo = new PaginationInfo(
            paginationOptions.PageNumber, 
            CurrentPageSize: (ushort) requestedPageValues.Length, 
            PageCountTotal: (ushort) totalPageCount);

        return new ExchangeRateHistory(
            startDate,
            endDate,
            requestedPageValues,
            paginationInfo);
    }

    private async Task<IReadOnlyCollection<ExchangeRate>> GetExchangeRatesFromApi(
        string baseCurrency, 
        string startDate, 
        string endDate, 
        CancellationToken cancellationToken = default)
    {
        var apiResponse = await _apiClient.GetHistoricalRatesAsync
            (startDate, endDate, baseCurrency, cancellationToken);


        return apiResponse.Rates
            .Select(rate => new ExchangeRate(rate.Key, rate.Value))
            .ToArray();
    }
    

    private string CreateHistoryCacheKey(
        string baseCurrency, 
        string startDate, 
        string endDate) =>
        $"{Id}_{baseCurrency}_{startDate}..{endDate}";
}

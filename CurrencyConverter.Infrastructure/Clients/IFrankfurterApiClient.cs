using System.Text.Json.Serialization;
using Refit;

namespace CurrencyConverter.Infrastructure.Clients;

public interface IFrankfurterApiClient
{
    [Get("/latest?base={baseCurrency}&symbols={targetCurrency}")]
    Task<LatestExchangeRatesFrankfurterApiResponse> GetLatestExchangeRatesAsync
        (string baseCurrency, string targetCurrency, CancellationToken cancellationToken);
    
    [Get("/latest?base={baseCurrency}")]
    Task<LatestExchangeRatesFrankfurterApiResponse> GetLatestExchangeRatesAsync
        (string baseCurrency, CancellationToken cancellationToken);
    
    [Get("/{startDate}..{endDate}?base={baseCurrency}")]
    Task<HistoricalRatesFrankfurterApiResponse> GetHistoricalRatesAsync
        (string startDate, string endDate, string baseCurrency, CancellationToken cancellationToken);

}

public sealed record LatestExchangeRatesFrankfurterApiResponse(
    [property:JsonPropertyName("base")] string Base,
    [property:JsonPropertyName("date")] string Date,
    [property:JsonPropertyName("rates")] Dictionary<string, decimal> Rates);

public sealed record HistoricalRatesFrankfurterApiResponse(    
    [property:JsonPropertyName("base")] string Base,
    [property:JsonPropertyName("start_date")] string StartDate,
    [property:JsonPropertyName("end_date")] string EndDate, 
    [property:JsonPropertyName("rates")] Dictionary<string, Dictionary<string, decimal>> Rates);
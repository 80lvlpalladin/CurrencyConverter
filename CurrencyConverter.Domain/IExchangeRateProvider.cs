namespace CurrencyConverter.Domain;

public interface IExchangeRateProvider
{
    /// <summary>
    /// Gets the unique identifier of the exchange rate provider.
    /// </summary>
    public string Id { get; }
    
    /// <summary>
    /// Asynchronously retrieves the latest exchange rate for the specified base currency against all currencies.
    /// </summary>
    /// <param name="baseCurrency">The base currency for which the exchange rate is to be retrieved (e.g., USD).</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A task that represents the asynchronous operation, containing the latest exchange rate information.</returns>
    public Task<ExchangeRate> GetLatestAsync(string baseCurrency, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Asynchronously converts an amount from the specified base currency to the target currency using the current exchange rate.
    /// </summary>
    /// <param name="baseCurrency">The currency from which the amount is to be converted (e.g., USD).</param>
    /// <param name="amount">The amount to be converted in the base currency.</param>
    /// <param name="targetCurrency">The currency to which the amount is to be converted (e.g., EUR).</param>
    /// <returns>A task that represents the asynchronous operation, containing the converted amount in the target currency.</returns>
    public Task<decimal> ConvertAsync(string baseCurrency, decimal amount, string targetCurrency, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves the historical exchange rates for the specified base currency against all available currencies.
    /// </summary>
    /// <param name="baseCurrency">The base currency for which the historical exchange rates are to be retrieved (e.g., USD).</param>
    /// <returns>A task that represents the asynchronous operation, containing the historical exchange rate information.</returns>
    public Task<ExchangeRateHistory> GetHistoryAsync(string baseCurrency, string startDate, string endDate, CancellationToken cancellationToken = default);
}

    
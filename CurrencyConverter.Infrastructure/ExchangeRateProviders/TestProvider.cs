using CurrencyConverter.Domain;

namespace CurrencyConverter.Infrastructure.ExchangeRateProviders;

//example of how to create another exchange rate provider
public class TestProvider : IExchangeRateProvider
{
    public string Id { get; } = "test";
    public Task<ExchangeRate> GetLatestAsync(string baseCurrency, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<decimal> ConvertAsync(string baseCurrency, decimal amount, string targetCurrency,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ExchangeRateHistory> GetHistoryAsync(string baseCurrency, string startDate, string endDate, PaginationOptions? paginationOptions = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
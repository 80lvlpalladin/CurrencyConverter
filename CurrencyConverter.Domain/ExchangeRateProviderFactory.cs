using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace CurrencyConverter.Domain;

public class ExchangeRateProviderFactory
{
    private readonly ILogger<ExchangeRateProviderFactory> _logger;
    private readonly Dictionary<string, IExchangeRateProvider> _exchangeRateProviderRegistry = new();
    private const string DefaultProviderId = "frankfurter";
    
    public ExchangeRateProviderFactory(
        IEnumerable<IExchangeRateProvider> exchangeRateProviders,
        ILogger<ExchangeRateProviderFactory> logger)
    {
        _logger = logger;
        //TODO test if this will work with multiple registered providers
        foreach (var exchangeRateProvider in exchangeRateProviders)
        {
            _exchangeRateProviderRegistry.Add(exchangeRateProvider.Id, exchangeRateProvider);
        }
    }

    public IExchangeRateProvider GetProvider(string? providerId)
    {
        if (TryGetExchangeRateProvider(providerId, out var exchangeRateProvider))
            return exchangeRateProvider;
        
        _logger.LogWarning( 
            "Exchange rate provider {ProviderId} not found. Using default provider.", 
            providerId);
            
        return _exchangeRateProviderRegistry[DefaultProviderId];
    }
    
    private bool TryGetExchangeRateProvider(
        string? providerId, 
        [NotNullWhen(true)] out IExchangeRateProvider? exchangeRateProvider)
    {
        if (providerId is not null && _exchangeRateProviderRegistry.TryGetValue(providerId, out var provider))
        {
            exchangeRateProvider = provider;
            return true;
        }

        exchangeRateProvider = null;
        return false;
    }
    
}
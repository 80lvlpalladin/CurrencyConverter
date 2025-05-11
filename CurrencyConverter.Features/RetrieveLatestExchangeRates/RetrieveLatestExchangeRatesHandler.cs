using CurrencyConverter.Domain;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CurrencyConverter.Features.RetrieveLatestExchangeRates;

public sealed record RetrieveLatestExchangeRatesRequest(string BaseCurrency, string? ExchangeRateProviderId) : 
    IRequest<ErrorOr<RetrieveLatestExchangeRatesResponse>>;

public sealed record RetrieveLatestExchangeRatesResponse
    (string ExchangeRateProvider, string BaseCurrency, string Date, Dictionary<string, decimal> Rates);

public class RetrieveLatestExchangeRatesHandler(
    ExchangeRateProviderFactory exchangeRateProviderFactory)
    : IRequestHandler<RetrieveLatestExchangeRatesRequest, ErrorOr<RetrieveLatestExchangeRatesResponse>>
{
    public async Task<ErrorOr<RetrieveLatestExchangeRatesResponse>> 
        Handle(RetrieveLatestExchangeRatesRequest request, CancellationToken cancellationToken)
    {
        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
        if(_requestValidator.Validate(request) is { IsValid : false } validationResult)
            return Error.Validation(validationResult.ToString());

        var exchangeRateProvider = 
            exchangeRateProviderFactory.GetProvider(request.ExchangeRateProviderId);

        var exchangeRate = 
            await exchangeRateProvider.GetLatestAsync(request.BaseCurrency, cancellationToken);
        
        return new RetrieveLatestExchangeRatesResponse(
            exchangeRateProvider.Id,
            request.BaseCurrency,
            exchangeRate.Date,
            exchangeRate.Rates);
    }
    
    
    private readonly RetrieveLatestExchangeRatesRequestValidator _requestValidator = new();
    
    private sealed class RetrieveLatestExchangeRatesRequestValidator
        : AbstractValidator<RetrieveLatestExchangeRatesRequest>
    {
        public RetrieveLatestExchangeRatesRequestValidator()
        {
            RuleFor(x => x.BaseCurrency)
                .NotEmpty()
                .Matches("^[A-Za-z]{3}$")
                .WithMessage("Base currency must be a three-letter currency code.");
        }
    }
}

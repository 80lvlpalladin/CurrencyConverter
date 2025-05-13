using CurrencyConverter.Domain;
using ErrorOr;
using FluentValidation;
using MediatR;

namespace CurrencyConverter.Features.RetrieveHistoricalExchangeRates;

public sealed record RetrieveHistoricalExchangeRatesRequest(
    string BaseCurrency,
    string StartDate,
    string EndDate,
    string? ExchangeRateProviderId,
    PaginationOptions? PaginationOptions) : IRequest<ErrorOr<RetrieveHistoricalExchangeRatesResponse>>;

public sealed record RetrieveHistoricalExchangeRatesResponse(
    string ExchangeRateProvider,
    string BaseCurrency,
    string StartDate,
    string EndDate,
    Dictionary<string, Dictionary<string, decimal>> Rates,
    PaginationInfo PaginationInfo);

public class RetrieveHistoricalExchangeRatesHandler(ExchangeRateProviderFactory exchangeRateProviderFactory)
    : IRequestHandler<RetrieveHistoricalExchangeRatesRequest, ErrorOr<RetrieveHistoricalExchangeRatesResponse>>
{
    private readonly RetrieveHistoricalExchangeRatesRequestValidator _requestValidator = new();
    
    public async Task<ErrorOr<RetrieveHistoricalExchangeRatesResponse>> 
        Handle(RetrieveHistoricalExchangeRatesRequest request, CancellationToken cancellationToken)
    {
        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
        if(_requestValidator.Validate(request) is { IsValid : false } validationResult)
            return Error.Validation(validationResult.ToString());

        var exchangeRateProvider = 
            exchangeRateProviderFactory.GetProvider(request.ExchangeRateProviderId);

        var exchangeRateHistory = await exchangeRateProvider.GetHistoryAsync(
            request.BaseCurrency,
            request.StartDate,
            request.EndDate,
            request.PaginationOptions,
            cancellationToken);

        var responseExchangeRates = exchangeRateHistory.Rates
            .ToDictionary(rate => rate.Date, rate => rate.Rates);
        
        return new RetrieveHistoricalExchangeRatesResponse(
            exchangeRateProvider.Id,
            request.BaseCurrency,
            request.StartDate,
            request.EndDate,
            responseExchangeRates,
            exchangeRateHistory.PaginationInfo);
    }
    
    private sealed class RetrieveHistoricalExchangeRatesRequestValidator
        : AbstractValidator<RetrieveHistoricalExchangeRatesRequest>
    {
        public RetrieveHistoricalExchangeRatesRequestValidator()
        {
            RuleFor(x => x.BaseCurrency)
                .NotEmpty()
                .Matches("^[A-Za-z]{3}$")
                .WithMessage("Base currency must be a three-letter currency code.");
            
            RuleFor(x => x.StartDate)
                .NotEmpty()
                .Matches(@"^\d{4}-\d{2}-\d{2}$")
                .WithMessage("Start date must be in the format YYYY-MM-DD.");
            
            RuleFor(x => x.EndDate)
                .NotEmpty()
                .Matches(@"^\d{4}-\d{2}-\d{2}$")
                .WithMessage("End date must be in the format YYYY-MM-DD.");

            RuleFor(x => x.PaginationOptions)
                .Must(po => 
                    po is null || (po is { PageNumber: > 0, MaxPageSize: > 1 } && po.PageNumber <= po.MaxPageSize))
                .WithMessage("Pagination options is not valid.");
            
            RuleFor(x => x)
                .Must(x => DateTime.Parse(x.StartDate) <= DateTime.Parse(x.EndDate))
                .WithMessage("Start date must be earlier than end date.");
            
        }
    }
}

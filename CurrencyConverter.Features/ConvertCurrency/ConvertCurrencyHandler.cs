using CurrencyConverter.Domain;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CurrencyConverter.Features.ConvertCurrency;

public sealed record ConvertCurrencyRequest(
    string BaseCurrency,
    string TargetCurrency,
    decimal Amount,
    string? ExchangeRateProviderId) : IRequest<ErrorOr<ConvertCurrencyResponse>>;

public sealed record ConvertCurrencyResponse(
    string ExchangeRateProvider, 
    string BaseCurrency,
    string TargetCurrency,
    decimal Value);

public sealed class ConvertCurrencyOptions
{
    public const string SectionName = "CurrencyConversion";
    public string[] ForbiddenCurrencies { get; set; } = [];
}

public class ConvertCurrencyHandler : IRequestHandler<ConvertCurrencyRequest, ErrorOr<ConvertCurrencyResponse>>
{
    private readonly ExchangeRateProviderFactory _exchangeRateProviderFactory;
    private readonly AbstractValidator<ConvertCurrencyRequest> _requestValidator;

    // ReSharper disable once ConvertToPrimaryConstructor
    public ConvertCurrencyHandler(
        ExchangeRateProviderFactory exchangeRateProviderFactory, 
        IOptions<ConvertCurrencyOptions> options)
    {
        _exchangeRateProviderFactory = exchangeRateProviderFactory;
        _requestValidator = new ConvertCurrencyRequestValidator(options.Value.ForbiddenCurrencies);
    }
    
    public async Task<ErrorOr<ConvertCurrencyResponse>> Handle
        (ConvertCurrencyRequest request, CancellationToken cancellationToken)
    {
        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
        if(_requestValidator.Validate(request) is { IsValid : false } validationResult)
            return Error.Validation(validationResult.ToString());
        
        var exchangeRateProvider = 
            _exchangeRateProviderFactory.GetProvider(request.ExchangeRateProviderId);
        
        var exchangeResult = await exchangeRateProvider.ConvertAsync(
            request.BaseCurrency, 
            request.Amount, 
            request.TargetCurrency, 
            cancellationToken);

        return new ConvertCurrencyResponse(
            exchangeRateProvider.Id,
            request.BaseCurrency,
            request.TargetCurrency,
            exchangeResult);
    }
    
    private sealed class ConvertCurrencyRequestValidator
        : AbstractValidator<ConvertCurrencyRequest>
    {
        internal ConvertCurrencyRequestValidator(string[] forbiddenCurrencies)
        {
            RuleFor(x => x.BaseCurrency)
                .NotEmpty()
                .Must(baseCurrency => 
                    !forbiddenCurrencies.Contains(baseCurrency, StringComparer.InvariantCultureIgnoreCase))
                .Matches("^[A-Za-z]{3}$")
                .WithMessage("Base currency must be a three-letter currency code and must not be among forbidden currencies.");
            
            RuleFor(x => x.TargetCurrency)
                .NotEmpty()
                .Must(baseCurrency => 
                    !forbiddenCurrencies.Contains(baseCurrency, StringComparer.InvariantCultureIgnoreCase))
                .Matches("^[A-Za-z]{3}$")
                .WithMessage("Target currency must be a three-letter currency code and must not be among forbidden currencies.");
        }
    }
}
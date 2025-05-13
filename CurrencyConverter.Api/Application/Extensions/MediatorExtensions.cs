using ErrorOr;
using MediatR;

namespace CurrencyConverter.Api.Application.Extensions;

public static class MediatorExtensions
{
    /// <summary>
    /// Sends a request using the mediator and processes the response to return an appropriate HTTP result.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response returned by the mediator.</typeparam>
    /// <param name="mediator">The <see cref="IMediator"/> instance used to send the request.</param>
    /// <param name="request">The request object implementing <see cref="IRequest{TResponse}"/>.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An <see cref="IResult"/> representing either a successful response or an error result based on the mediator's response.</returns>
    public static async Task<IResult> SendAndReturnResultAsync<TResponse>(
        this IMediator mediator,
        IRequest<ErrorOr<TResponse>> request,
        CancellationToken cancellationToken = default)
    {
        var errorOrResult = await mediator.Send(request, cancellationToken);

        return errorOrResult.IsError ?
            CreateProblemResult(errorOrResult.Errors) :
            TypedResults.Ok(errorOrResult.Value);
    }

    private static IResult CreateProblemResult(IReadOnlyCollection<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.All(error => error.Type == ErrorType.Validation))
        {
            var modelStateDictionary = errors.GroupBy(error => error.Code).ToDictionary(
                errorGroup => errorGroup.Key,
                errorGroup => errorGroup.Select(error => error.Description).ToArray());

            return TypedResults.ValidationProblem(modelStateDictionary);
        }

        var firstError = errors.First();
        var statusCode = firstError.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return TypedResults.Problem(statusCode: statusCode, title: firstError.Description);
    }
}

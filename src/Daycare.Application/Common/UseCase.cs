using Daycare.Application.Common.Exceptions;
using FluentValidation;

namespace Daycare.Application.Common;

public abstract class UseCase<TRequest, TResponse>
{
    private readonly IValidator<TRequest>? _validator;

    protected UseCase(IValidator<TRequest>? validator = null)
    {
        _validator = validator;
    }

    public async Task<TResponse> ExecuteAsync(TRequest request, CancellationToken ct = default)
    {
        if (_validator != null)
        {
            var validationResult = await _validator.ValidateAsync(request, ct);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                    .ToDictionary(
                        g => ToCamelCase(g.Key),
                        g => g.ToArray());

                throw new ValidationFailedException(errors);
            }
        }

        return await HandleAsync(request, ct);
    }

    protected abstract Task<TResponse> HandleAsync(TRequest request, CancellationToken ct);

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
            return str;

        return char.ToLowerInvariant(str[0]) + str[1..];
    }
}

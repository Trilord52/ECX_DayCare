namespace Daycare.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public string Code { get; } = "NOT_FOUND";

    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.")
    {
    }
}

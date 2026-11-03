namespace Daycare.Application.Common.Exceptions;

public class ForbiddenException : Exception
{
    public string Code { get; } = "FORBIDDEN";

    public ForbiddenException(string message = "You do not have permission to perform this action.")
        : base(message)
    {
    }
}

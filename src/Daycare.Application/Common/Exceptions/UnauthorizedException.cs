namespace Daycare.Application.Common.Exceptions;

public class UnauthorizedException : Exception
{
    public string Code { get; }

    public UnauthorizedException(string message, string code = "INVALID_CREDENTIALS")
        : base(message)
    {
        Code = code;
    }
}

namespace Daycare.Domain.Exceptions;

public class DomainRuleException : Exception
{
    public string Code { get; }

    public DomainRuleException(string code, string message) : base(message)
    {
        Code = code;
    }
}

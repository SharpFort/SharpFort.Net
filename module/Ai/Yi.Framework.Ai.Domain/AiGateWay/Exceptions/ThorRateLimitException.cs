namespace Yi.Framework.Ai.Domain.AiGateWay.Exceptions;

public class ThorRateLimitException : Exception
{
    public ThorRateLimitException()
    {
    }

    public ThorRateLimitException(string message) : base(message)
    {
    }
}

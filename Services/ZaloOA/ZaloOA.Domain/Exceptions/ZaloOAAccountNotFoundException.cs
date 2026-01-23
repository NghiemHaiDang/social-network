namespace ZaloOA.Domain.Exceptions;

public class ZaloOAAccountNotFoundException : DomainException
{
    public ZaloOAAccountNotFoundException(Guid id)
        : base($"Zalo OA account with ID '{id}' was not found.")
    {
    }

    public ZaloOAAccountNotFoundException(string userId, string oaId)
        : base($"Zalo OA account for user '{userId}' with OA ID '{oaId}' was not found.")
    {
    }
}

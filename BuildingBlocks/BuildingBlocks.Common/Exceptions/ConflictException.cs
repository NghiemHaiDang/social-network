namespace BuildingBlocks.Common.Exceptions;

public class ConflictException : Exception
{
    public ConflictException() : base("Resource conflict occurred.")
    {
    }

    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string name, object key)
        : base($"Entity \"{name}\" ({key}) already exists.")
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

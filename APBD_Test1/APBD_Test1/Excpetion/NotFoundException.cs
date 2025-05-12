namespace APBD_Test1.Excpetion;

public class NotFoundException : Exception
{
    public NotFoundException(string? message) : base(message)
    {
    }
}
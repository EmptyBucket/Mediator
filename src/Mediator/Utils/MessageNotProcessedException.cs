namespace Mediator.Utils;

public class MessageNotProcessedException : InvalidOperationException
{
    public MessageNotProcessedException() : base("Message was not be processed")
    {
    }
}
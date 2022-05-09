namespace Mediator.Utils;

public record MessageContext(IServiceProvider ServiceProvider, string RoutingKey = "")
{
}
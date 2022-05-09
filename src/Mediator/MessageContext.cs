namespace Mediator;

public record MessageContext(IServiceProvider ServiceProvider, string RoutingKey = "")
{
}
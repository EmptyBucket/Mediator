namespace Mediator.Handlers;

public record MessageContext(IServiceProvider ServiceProvider, string RoutingKey = "")
{
}
namespace FlexMediator.Utils;

public record MessageContext(IServiceProvider ServiceProvider, string RoutingKey = "")
{
}
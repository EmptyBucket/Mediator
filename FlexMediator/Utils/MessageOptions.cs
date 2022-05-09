namespace FlexMediator.Utils;

public record MessageOptions(IServiceProvider ServiceProvider, string RoutingKey = "")
{
}
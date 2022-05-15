namespace Mediator.Handlers;

public class MessageContext
{
	public MessageContext(IServiceProvider serviceProvider, string routingKey = "")
	{
		ServiceProvider = serviceProvider;
		RoutingKey = routingKey;
	}

	public IServiceProvider ServiceProvider { get; set; }
	
	public string RoutingKey { get; set; }
}
using Microsoft.Extensions.Logging;

namespace ConsoleApp5.Pipes;

public class LoggingPipe : IPipe
{
    private readonly IPipe _nextPipe;
    private readonly ILogger<LoggingPipe> _logger;

    public LoggingPipe(IPipe nextPipe, ILogger<LoggingPipe> logger)
    {
        _nextPipe = nextPipe;
        _logger = logger;
    }

    public async Task Handle<TMessage>(TMessage message, MessageOptions options, CancellationToken token)
    {
        _logger.LogInformation($"Publishing message {message}");
        await _nextPipe.Handle(message, options, token);
        _logger.LogInformation($"Published message {message}");
    }

    public async Task<TResult> Handle<TMessage, TResult>(TMessage message, MessageOptions options,
        CancellationToken token)
    {
        _logger.LogInformation($"Sending message {message}");
        var result = await _nextPipe.Handle<TMessage, TResult>(message, options, token);
        _logger.LogInformation($"Sent message {message}");
        return result;
    }
}
using System.Collections.Concurrent;
using System.Text.Json;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.PublishSubscribe;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Mediator.Redis.Pipes;

public class RedisStreamPipeConnector : IPubPipeConnector
{
    private readonly IDatabase _database;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<PipeConnection, PipeConnection> _pipeConnections = new();

    public RedisStreamPipeConnector(IConnectionMultiplexer connectionMultiplexer, IServiceProvider serviceProvider)
    {
        _database = connectionMultiplexer.GetDatabase();
        _serviceProvider = serviceProvider;
    }

    public async Task<IAsyncDisposable> ConnectOutAsync<TMessage>(IPubPipe pipe, string routingKey = "",
        string subscriptionId = "", CancellationToken token = default)
    {
        var route = Route.For<TMessage>(routingKey);
        await _database.StreamCreateConsumerGroupAsync(route.ToString(), subscriptionId, "0");
        var cts = new CancellationTokenSource();
        Task.Run(async () =>
        {
            while (cts.IsCancellationRequested)
            {
                var entries =
                    await _database.StreamReadGroupAsync(route.ToString(), subscriptionId, subscriptionId, ">");

                foreach (var entry in entries)
                {
                    await using var scope = _serviceProvider.CreateAsyncScope();
                    var message = JsonSerializer.Deserialize<TMessage>(entry["message"]);
                    await pipe.PassAsync(message, new MessageContext(scope.ServiceProvider, routingKey), cts.Token);
                }
            }
        }, token);
        var pipeConnection = new PipeConnection(cts, Disconnect);
        Connect(pipeConnection);
        return pipeConnection;
    }

    private void Connect(PipeConnection pipeConnection) => _pipeConnections.TryAdd(pipeConnection, pipeConnection);

    private void Disconnect(PipeConnection pipeConnection) => _pipeConnections.TryRemove(pipeConnection, out _);

    public async ValueTask DisposeAsync()
    {
        foreach (var pipeConnection in _pipeConnections.Values) await pipeConnection.DisposeAsync();
    }

    private class PipeConnection : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly Action<PipeConnection> _callback;

        public PipeConnection(CancellationTokenSource cts, Action<PipeConnection> callback)
        {
            _cts = cts;
            _callback = callback;
        }

        public ValueTask DisposeAsync()
        {
            _cts.Cancel();
            _cts.Dispose();
            _callback(this);
            return ValueTask.CompletedTask;
        }
    }
}
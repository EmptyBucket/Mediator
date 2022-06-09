using System.Text.Json;
using Mediator.Handlers;
using Mediator.Pipes;
using Mediator.Pipes.Utils;
using Mediator.Utils;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Mediator.Redis.Pipes;

public class RedisStreamReqPipe : IConnectingReqPipe
{
    private readonly IDatabase _database;
    private readonly IServiceProvider _serviceProvider;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public RedisStreamReqPipe(IConnectionMultiplexer multiplexer, IServiceProvider serviceProvider)
    {
        _database = multiplexer.GetDatabase();
        _serviceProvider = serviceProvider;
        _jsonSerializerOptions = new JsonSerializerOptions { Converters = { ObjectToInferredTypesConverter.Instance } };
    }

    public Task<TResult> PassAsync<TMessage, TResult>(MessageContext<TMessage> ctx, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public IDisposable ConnectOut<TMessage, TResult>(IReqPipe pipe, string routingKey = "")
    {
        throw new NotImplementedException();
    }

    public Task<IAsyncDisposable> ConnectOutAsync<TMessage, TResult>(IReqPipe pipe, string routingKey = "",
        CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }

    private async Task HandleAsync<TMessage, TResult>(Route route, IReqPipe pipe, string groupName, string consumerName,
        string position, int count)
    {
        StreamEntry[] entries;

        do
        {
            entries = await _database
                .StreamReadGroupAsync(route.ToString(), groupName, consumerName, position, count)
                .ConfigureAwait(false);
            var now = DateTime.Now;

            foreach (var entry in entries)
            {
                var ctx = JsonSerializer.Deserialize<MessageContext<TMessage>>(entry[WellKnown.MessageKey],
                    _jsonSerializerOptions)!;
                TResult? result = default;
                Exception? exception = default;

                try
                {
                    await using var scope = _serviceProvider.CreateAsyncScope();
                    ctx = ctx with { MessageId = entry.Id, DeliveredAt = now, ServiceProvider = scope.ServiceProvider };
                    result = await pipe.PassAsync<TMessage, TResult>(ctx);
                    await _database.StreamAcknowledgeAsync(route.ToString(), groupName, entry.Id).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    exception = e;
                    throw;
                }
                finally
                {
                    var resultCtx = new ResultContext<TResult>(ctx.Route, Guid.NewGuid().ToString(), ctx.CorrelationId!)
                        { Result = result, Exception = exception };
                    var resultMessage = JsonSerializer.Serialize(resultCtx, _jsonSerializerOptions);
                    await _database.StreamAddAsync(WellKnown.ResultStream, WellKnown.MessageKey, resultMessage)
                        .ConfigureAwait(false);
                }
            }
        } while (entries.Any());
    }

    private async Task<ChannelMessageQueue> CreateResultMqAsync()
    {
        async Task HandleAsync<TMessage>(Route route, IPubPipe pipe, string groupName, string consumerName,
            string position, int count)
        {
            StreamEntry[] entries;

            do
            {
                entries = await _database
                    .StreamReadGroupAsync(route.ToString(), groupName, consumerName, position, count)
                    .ConfigureAwait(false);
                var now = DateTime.Now;

                foreach (var entry in entries)
                {
                    var ctx = JsonSerializer.Deserialize<MessageContext<TMessage>>(entry[WellKnown.MessageKey],
                        _jsonSerializerOptions)!;
                    await using var scope = _serviceProvider.CreateAsyncScope();
                    ctx = ctx with { MessageId = entry.Id, DeliveredAt = now, ServiceProvider = scope.ServiceProvider };
                    await pipe.PassAsync(ctx);
                    await _database.StreamAcknowledgeAsync(route.ToString(), groupName, entry.Id).ConfigureAwait(false);
                }
            } while (entries.Any());
        }

        // код, который крутит цикл
    }
}
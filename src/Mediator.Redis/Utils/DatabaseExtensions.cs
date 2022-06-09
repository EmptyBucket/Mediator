using Mediator.Pipes.Utils;
using StackExchange.Redis;

namespace Mediator.Redis.Utils;

internal static class DatabaseExtensions
{
    private static string ConsumerGroupExistsExceptionMessage => "BUSYGROUP Consumer Group name already exists";

    public static bool TryCreateConsumerGroup(this IDatabase database, Route route, string groupName, string position)
    {
        try
        {
            return database.StreamCreateConsumerGroup(route.ToString(), groupName, position);
        }
        catch (RedisException e)
        {
            if (e.Message == ConsumerGroupExistsExceptionMessage) return false;
            throw;
        }
    }

    public static async Task<bool> TryCreateConsumerGroupAsync(this IDatabase database, Route route, string groupName,
        string position)
    {
        try
        {
            return await database.StreamCreateConsumerGroupAsync(route.ToString(), groupName, position)
                .ConfigureAwait(false);
        }
        catch (RedisException e)
        {
            if (e.Message == ConsumerGroupExistsExceptionMessage) return false;
            throw;
        }
    }
}
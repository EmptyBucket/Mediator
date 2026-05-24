using Mediator.Pipes.Utils;

namespace Mediator.Nats.Utils;

public static class RouteExtensions
{
    public static string ToStreamName(this Route route)
    {
        var chars = route.ToString().ToCharArray();

        for (var i = 0; i < chars.Length; i++)
            chars[i] = chars[i] switch { '.' or '*' or '>' or ' ' or '\t' => '_', _ => chars[i] };

        return new string(chars);
    }
}
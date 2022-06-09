using System.Reflection;

namespace Mediator.Utils;

internal static class ObjectExtensions
{
    private static readonly MethodInfo CastMethodInfo =
        typeof(ObjectExtensions).GetMethod(nameof(Cast), BindingFlags.Static | BindingFlags.NonPublic)!;

    public static object CastTo(this object obj, Type type) =>
        CastMethodInfo.MakeGenericMethod(type).Invoke(null, new[] { obj });

    private static T Cast<T>(dynamic o) => (T)o;
}
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Moq;

namespace Mediator.Tests;

public static class MockExtensions
{
    public static async Task VerifyAsync<T>(this Mock<T> mock, Expression<Action<T>> expression, Times times,
        TimeSpan? timeout = null)
        where T : class
    {
        timeout ??= TimeSpan.FromSeconds(1);

        Exception exception = null;

        var stopWatch = new Stopwatch();
        stopWatch.Start();

        while (stopWatch.Elapsed < timeout)
            try
            {
                mock.Verify(expression, times);
                return;
            }
            catch (MockException e)
            {
                exception = e;
                await Task.Delay(100);
            }

        throw exception!;
    }
}
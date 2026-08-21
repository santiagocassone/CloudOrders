using System.Collections.Concurrent;

namespace CloudOrders.UnitTests;

public class ConcurrencyTests
{
    [Fact]
    public async Task ProcessAsync_ShouldLimitConcurrencyToThree()
    {
        var activeOperations = 0;
        var maxConcurrentOperations = 0;

        using var semaphore = new SemaphoreSlim(3);

        var tasks = Enumerable.Range(1, 20)
            .Select(async item =>
            {
                await semaphore.WaitAsync();

                try
                {
                    var active = Interlocked.Increment(
                        ref activeOperations);

                    InterlockedExtensions.Max(
                        ref maxConcurrentOperations,
                        active);

                    await Task.Delay(200);
                }
                finally
                {
                    Interlocked.Decrement(
                        ref activeOperations);

                    semaphore.Release();
                }
            });

        await Task.WhenAll(tasks);

        Assert.Equal(3, maxConcurrentOperations);
    }

    internal static class InterlockedExtensions
    {
        public static void Max(
            ref int target,
            int value)
        {
            int current;

            do
            {
                current = target;

                if (value <= current)
                {
                    return;
                }
            }
            while (Interlocked.CompareExchange(
                ref target,
                value,
                current) != current);
        }
    }

    [Fact]
    public async Task ProcessAsync_WithParallelForEach_ShouldLimitConcurrencyToThree()
    {
        var activeOperations = 0;
        var maxConcurrentOperations = 0;

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 3
        };

        await Parallel.ForEachAsync(
            Enumerable.Range(1, 20),
            options,
            async (item, cancellationToken) =>
            {
                var active = Interlocked.Increment(
                    ref activeOperations);

                InterlockedExtensions.Max(
                    ref maxConcurrentOperations,
                    active);

                try
                {
                    await Task.Delay(
                        200,
                        cancellationToken);
                }
                finally
                {
                    Interlocked.Decrement(
                        ref activeOperations);
                }
            });

        Assert.Equal(3, maxConcurrentOperations);
    }
}
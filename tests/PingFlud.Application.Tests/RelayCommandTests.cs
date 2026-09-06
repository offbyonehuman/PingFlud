using PingFlud.Application;
using Xunit;

namespace PingFlud.Application.Tests;

public sealed class RelayCommandTests
{
    [Fact]
    public async Task ExecuteAsyncPreventsConcurrentExecutions()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var invocationCount = 0;
        var command = new AsyncRelayCommand(async () =>
        {
            invocationCount++;
            await release.Task;
        });

        var first = command.ExecuteAsync();
        var second = command.ExecuteAsync();

        Assert.Equal(1, invocationCount);
        Assert.True(second.IsCompletedSuccessfully);
        Assert.False(command.CanExecute(null));

        release.SetResult();
        await first;

        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task ExecuteReportsUnhandledFailuresWithoutEscapingAsyncVoid()
    {
        var observed = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = new InvalidOperationException("synthetic failure");
        var command = new AsyncRelayCommand(() => Task.FromException(expected));
        command.UnhandledException += exception => observed.TrySetResult(exception);

        command.Execute(null);

        Assert.Same(expected, await observed.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.True(command.CanExecute(null));
    }
}

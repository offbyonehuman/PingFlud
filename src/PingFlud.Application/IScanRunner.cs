using PingFlud.Core;

namespace PingFlud.Application;

public interface IUiDispatcher
{
    void Invoke(Action action);
}

public sealed class ImmediateUiDispatcher : IUiDispatcher
{
    public static ImmediateUiDispatcher Instance { get; } = new();

    public void Invoke(Action action) => action();
}

public interface IScanRunner
{
    Task ScanAsync(
        IReadOnlyList<string> targets,
        ScanSettings settings,
        IProgress<ScanResult> progress,
        CancellationToken cancellationToken);
}

public sealed class PingScanRunner(PingScanner? scanner = null) : IScanRunner
{
    private readonly PingScanner _scanner = scanner ?? new PingScanner();

    public Task ScanAsync(
        IReadOnlyList<string> targets,
        ScanSettings settings,
        IProgress<ScanResult> progress,
        CancellationToken cancellationToken) =>
        _scanner.ScanAsync(targets, settings, progress, cancellationToken);
}

using PingFlud.Application;
using PingFlud.Core;
using Xunit;

namespace PingFlud.Application.Tests;

public sealed class MainViewModelTests
{
    [Fact]
    public void StartsInReadyEmptyState()
    {
        var model = new MainViewModel(new FakeScanRunner());

        Assert.Equal("Ready", model.Status);
        Assert.Equal("0 targets", model.Summary);
        Assert.False(model.CanStart);
        Assert.True(model.CanEditTargets);
        Assert.False(model.CanStop);
        Assert.False(model.HasResults);
        Assert.Empty(model.Results);
    }

    [Fact]
    public void CanStartRequiresTargets()
    {
        var model = new MainViewModel(new FakeScanRunner()) { Targets = "localhost" };

        Assert.True(model.CanStart);
    }

    [Fact]
    public async Task StreamingResultsBatchesVisibleCollectionRefreshes()
    {
        var rows = Enumerable.Range(1, 101)
            .Select(index =>
            {
                var address = $"10.0.0.{index}";
                return new ScanResult(address, true, 1, string.Empty, address, "Responding", 1, 1, 0, 128);
            })
            .ToArray();
        var model = new MainViewModel(new FakeScanRunner(rows)) { Targets = "10.0.0.0/24" };
        var collectionChanges = 0;
        model.Results.CollectionChanged += (_, _) => collectionChanges++;

        await model.StartScanAsync();

        Assert.Equal(101, model.TotalResultCount);
        Assert.True(collectionChanges < 500, $"Expected batched refreshes, observed {collectionChanges} collection events.");
    }

    [Fact]
    public void ChangingTargetsNotifiesStartAvailability()
    {
        var model = new MainViewModel(new FakeScanRunner());
        var changedProperties = new List<string?>();
        model.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        model.Targets = "localhost";

        Assert.True(model.CanStart);
        Assert.Contains(nameof(MainViewModel.CanStart), changedProperties);
    }

    [Fact]
    public async Task ExportUsesVisibleFilteredResults()
    {
        var model = new MainViewModel(new FakeScanRunner(
            new ScanResult("up", true, 1, "", "127.0.0.1", "Responding", 1, 1, 0, 128),
            new ScanResult("down", false, null, "", "127.0.0.2", "TimedOut", 1, 0, 100, null)))
        { Targets = "up,down" };
        await model.StartScanAsync();
        model.Filter = ResultFilter.Responding;

        var path = Path.Combine(Path.GetTempPath(), $"pingflud-visible-export-{Guid.NewGuid():N}.csv");
        try
        {
            model.ExportPath = path;
            await ((AsyncRelayCommand)model.ExportCommand).ExecuteAsync();

            var content = await File.ReadAllTextAsync(path);
            Assert.Contains("up", content);
            Assert.DoesNotContain("down", content);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task ExportIsDisabledWhenFilterHidesEveryResult()
    {
        var model = new MainViewModel(new FakeScanRunner(
            new ScanResult("up", true, 1, "", "127.0.0.1", "Responding", 1, 1, 0, 128)))
        { Targets = "up" };
        await model.StartScanAsync();

        model.Search = "does-not-match";

        Assert.True(model.HasResults);
        Assert.Empty(model.Results);
        Assert.False(model.CanCopyResults);
        Assert.True(model.CanClearResults);
        Assert.False(model.CanExport);
    }

    [Fact]
    public void CanExportRequiresResults()
    {
        var model = new MainViewModel(new FakeScanRunner()) { Targets = "localhost" };

        Assert.False(model.CanExport);

        model.ApplyTestResult(new ScanResult("localhost", true, 1, "", "127.0.0.1", "Responding", 1, 1, 0, 128));

        Assert.True(model.CanExport);
    }

    [Fact]
    public async Task ExportCommandWritesResultsAndResetsState()
    {
        var model = new MainViewModel(new FakeScanRunner(
            new ScanResult("host", true, 12, "host.example", "10.0.0.1", "Responding", 1, 1, 0, 64)))
        { Targets = "host" };
        await model.StartScanAsync();

        var path = Path.Combine(Path.GetTempPath(), $"pingflud-export-{Guid.NewGuid():N}.csv");
        try
        {
            model.ExportKind = ExportKind.Csv;
            model.ExportPath = path;

            await ((AsyncRelayCommand)model.ExportCommand).ExecuteAsync();

            Assert.True(File.Exists(path));
            Assert.Equal("Export complete", model.Status);
            Assert.False(model.IsExporting);
            Assert.True(model.CanStart);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ExportCommandCannotRunWithoutResults()
    {
        var model = new MainViewModel(new FakeScanRunner()) { Targets = "host" };

        Assert.False(((AsyncRelayCommand)model.ExportCommand).CanExecute(null));
    }

    [Fact]
    public async Task ScanStreamsResultsAndReplacesDnsEnrichment()
    {
        var runner = new FakeScanRunner(
            new ScanResult("localhost", true, 1, "", "127.0.0.1", "Responding", 1, 1, 0, 128),
            new ScanResult("localhost", true, 1, "localhost", "127.0.0.1", "Responding", 1, 1, 0, 128));
        var model = new MainViewModel(runner) { Targets = "localhost" };
        var summaries = new List<string>();
        model.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.Summary)) summaries.Add(model.Summary);
        };

        await model.StartScanAsync();

        var result = Assert.Single(model.Results);
        Assert.Equal("localhost", result.HostName);
        Assert.DoesNotContain(summaries, summary => summary.Contains("2 responding", StringComparison.Ordinal));
        Assert.Equal("Scan complete", model.Status);
        Assert.Equal("1 target • 1 responding • 0 unavailable • median 1 ms", model.Summary);
        Assert.Equal(100, model.ProgressPercent);
        Assert.True(model.CanStart);
        Assert.False(model.CanStop);
    }

    [Fact]
    public async Task ExistingResultsCannotBeChangedWhileScanning()
    {
        var runner = new BlockingScanRunner();
        var model = new MainViewModel(runner)
        {
            Targets = "next-scan"
        };
        model.ApplyTestResult(new ScanResult("old", true, 1, "", "127.0.0.1", "Responding", 1, 1, 0, 128));

        var scanTask = model.StartScanAsync();
        await runner.Started.Task;

        Assert.False(model.CanCopyResults);
        Assert.False(model.CanClearResults);
        Assert.False(model.CanExport);

        runner.Release.TrySetResult();
        await scanTask;
    }

    [Fact]
    public async Task SmallScansPublishResultsAsTheyArrive()
    {
        var runner = new StreamingScanRunner(
            new ScanResult("first", true, 1, "", "127.0.0.1", "Responding", 1, 1, 0, 128),
            new ScanResult("second", false, 1, "", "192.0.2.1", "TimedOut", 0, 1, 100, 0));
        var model = new MainViewModel(runner) { Targets = "first,second" };

        var scanTask = model.StartScanAsync();
        await runner.FirstReported.Task;

        Assert.Single(model.Results);

        runner.Release.TrySetResult();
        await scanTask;
    }

    [Fact]
    public async Task ScanUsesSettingsSnapshot()
    {
        var runner = new SettingsSnapshotRunner();
        var settings = new ScanSettings { TimeoutMs = 1000 };
        var model = new MainViewModel(runner)
        {
            Settings = settings,
            Targets = "127.0.0.1"
        };

        var scanTask = model.StartScanAsync();
        await runner.Started.Task;

        settings.TimeoutMs = 2000;

        Assert.NotNull(runner.ReceivedSettings);
        Assert.Equal(1000, runner.ReceivedSettings.TimeoutMs);

        runner.Release.TrySetResult();
        await scanTask;
    }

    [Fact]
    public async Task NullTargetsAreTreatedAsEmptyInput()
    {
        var model = new MainViewModel(new FakeScanRunner()) { Targets = null! };

        Assert.Equal(string.Empty, model.Targets);
        Assert.False(model.CanStart);

        await model.StartScanAsync();

        Assert.Equal("Cannot start scan", model.Status);
    }

    [Fact]
    public async Task FilterAndSearchApplyToStreamedResults()
    {
        var runner = new FakeScanRunner(
            new ScanResult("one", true, 1, "alpha", "127.0.0.1", "Responding", 1, 1, 0, 128),
            new ScanResult("two", false, null, "", "127.0.0.2", "TimedOut", 1, 0, 100, null));
        var model = new MainViewModel(runner) { Targets = "one,two" };
        await model.StartScanAsync();

        model.Filter = ResultFilter.Responding;
        model.Search = "alpha";

        var result = Assert.Single(model.Results);
        Assert.Equal("one", result.Target);
        Assert.Equal(2, model.TotalResultCount);
        Assert.True(model.HasResults);
    }

    [Fact]
    public async Task ResultsSortByAddressAndToggleDirection()
    {
        var model = new MainViewModel(new FakeScanRunner(
            new ScanResult("ten", true, 1, "", "127.0.0.10", "Responding", 1, 1, 0, 128),
            new ScanResult("two", true, 1, "", "127.0.0.2", "Responding", 1, 1, 0, 128)))
        { Targets = "ten,two" };

        await model.StartScanAsync();
        Assert.Equal(["127.0.0.2", "127.0.0.10"], model.Results.Select(row => row.Address));

        model.SortBy(nameof(ScanResult.Address));
        Assert.Equal(["127.0.0.10", "127.0.0.2"], model.Results.Select(row => row.Address));
    }

    [Fact]
    public async Task ClearResultsResetsCompletedScan()
    {
        var model = new MainViewModel(new FakeScanRunner(
            new ScanResult("one", true, 1, "", "127.0.0.1", "Responding", 1, 1, 0, 128)))
        { Targets = "one" };
        await model.StartScanAsync();

        var cleared = model.ClearResults();

        Assert.True(cleared);
        Assert.False(model.HasResults);
        Assert.Empty(model.Results);
        Assert.Equal("Ready", model.Status);
        Assert.Equal("0 targets", model.Summary);
    }

    [Fact]
    public async Task ScanMarshalsStreamedResultsThroughDispatcher()
    {
        var dispatcher = new RecordingDispatcher();
        var model = new MainViewModel(new BackgroundScanRunner(), dispatcher) { Targets = "localhost" };

        await model.StartScanAsync();

        Assert.True(dispatcher.InvocationCount > 0);
        Assert.Single(model.Results);
    }

    private sealed class RecordingDispatcher : IUiDispatcher
    {
        public int InvocationCount { get; private set; }

        public void Invoke(Action action)
        {
            InvocationCount++;
            action();
        }
    }

    private sealed class BackgroundScanRunner : IScanRunner
    {
        public Task ScanAsync(
            IReadOnlyList<string> targets,
            ScanSettings settings,
            IProgress<ScanResult> progress,
            CancellationToken cancellationToken) =>
            Task.Run(() => progress.Report(
                new ScanResult("localhost", true, 1, "", "127.0.0.1", "Responding", 1, 1, 0, 128)), cancellationToken);
    }

    private sealed class BlockingScanRunner : IScanRunner
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task ScanAsync(
            IReadOnlyList<string> targets,
            ScanSettings settings,
            IProgress<ScanResult> progress,
            CancellationToken cancellationToken)
        {
            Started.SetResult();
            await Release.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class StreamingScanRunner(ScanResult first, ScanResult second) : IScanRunner
    {
        public TaskCompletionSource FirstReported { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task ScanAsync(
            IReadOnlyList<string> targets,
            ScanSettings settings,
            IProgress<ScanResult> progress,
            CancellationToken cancellationToken)
        {
            progress.Report(first);
            FirstReported.SetResult();
            await Release.Task.WaitAsync(cancellationToken);
            progress.Report(second);
        }
    }

    private sealed class SettingsSnapshotRunner : IScanRunner
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ScanSettings? ReceivedSettings { get; private set; }

        public async Task ScanAsync(
            IReadOnlyList<string> targets,
            ScanSettings settings,
            IProgress<ScanResult> progress,
            CancellationToken cancellationToken)
        {
            ReceivedSettings = settings;
            Started.SetResult();
            await Release.Task.WaitAsync(cancellationToken);
        }
    }

    private sealed class FakeScanRunner(params ScanResult[] rows) : IScanRunner
    {
        public Task ScanAsync(
            IReadOnlyList<string> targets,
            ScanSettings settings,
            IProgress<ScanResult> progress,
            CancellationToken cancellationToken)
        {
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress.Report(row);
            }

            return Task.CompletedTask;
        }
    }
}

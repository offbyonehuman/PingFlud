using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using PingFlud.Core;

namespace PingFlud.Application;

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IScanRunner _scanner;
    private readonly IUiDispatcher _dispatcher;
    private readonly List<ScanResult> _allResults = [];
    private readonly Dictionary<string, int> _resultIndexes = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _scanCancellation;
    private string _targets = string.Empty;
    private string _status = "Ready";
    private string _summary = "0 targets";
    private int _progressPercent;
    private bool _isScanning;
    private int _completed;
    private int _total;
    private int _respondingCount;
    private int _pendingVisibleRefreshes;
    private DateTime _nextVisibleRefreshUtc = DateTime.MinValue;
    private ResultFilter _filter;
    private string _search = string.Empty;
    private string _sortProperty = nameof(ScanResult.Address);
    private bool _sortAscending = true;

    public MainViewModel(IScanRunner scanner, IUiDispatcher? dispatcher = null)
    {
        _scanner = scanner;
        _dispatcher = dispatcher ?? ImmediateUiDispatcher.Instance;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly ResettableObservableCollection<ScanResult> _visibleResults = [];
    public ObservableCollection<ScanResult> Results => _visibleResults;

    public int TotalResultCount => _allResults.Count;
    public bool HasResults => TotalResultCount > 0;
    public string SortProperty => _sortProperty;
    public bool SortAscending => _sortAscending;

    public ResultFilter Filter
    {
        get => _filter;
        set
        {
            if (!SetField(ref _filter, value)) return;
            RebuildVisibleResults();
        }
    }

    public string Search
    {
        get => _search;
        set
        {
            if (!SetField(ref _search, value ?? string.Empty)) return;
            RebuildVisibleResults();
        }
    }

    public ScanSettings Settings { get; set; } = new();

    public string Targets
    {
        get => _targets;
        set
        {
            if (!SetField(ref _targets, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanStart));
        }
    }

    public string Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    public string Summary
    {
        get => _summary;
        private set => SetField(ref _summary, value);
    }

    public int ProgressPercent
    {
        get => _progressPercent;
        private set => SetField(ref _progressPercent, value);
    }

    public bool IsScanning
    {
        get => _isScanning;
        private set
        {
            if (!SetField(ref _isScanning, value)) return;
            OnPropertyChanged(nameof(CanStart));
            OnPropertyChanged(nameof(CanStop));
            OnPropertyChanged(nameof(CanEditTargets));
            OnPropertyChanged(nameof(CanCopyResults));
            OnPropertyChanged(nameof(CanClearResults));
            OnPropertyChanged(nameof(CanExport));
        }
    }

    public bool CanStart => !IsScanning && !string.IsNullOrWhiteSpace(Targets) && !IsExporting;
    public bool CanStop => IsScanning;
    public bool CanEditTargets => !IsScanning && !IsExporting;
    public bool CanCopyResults => HasVisibleResults && !IsScanning && !IsExporting;
    public bool CanClearResults => HasResults && !IsScanning && !IsExporting;
    public bool CanExport => HasVisibleResults && !IsScanning && !IsExporting;

    public bool HasVisibleResults => Results.Count > 0;

    private ExportKind _exportKind = ExportKind.Csv;
    public ExportKind ExportKind
    {
        get => _exportKind;
        set => SetField(ref _exportKind, value);
    }

    private string _exportPath = string.Empty;
    public string ExportPath
    {
        get => _exportPath;
        set => SetField(ref _exportPath, value ?? string.Empty);
    }

    public bool IsExporting
    {
        get => _isExporting;
        private set
        {
            if (!SetField(ref _isExporting, value)) return;
            OnPropertyChanged(nameof(CanStart));
            OnPropertyChanged(nameof(CanEditTargets));
            OnPropertyChanged(nameof(CanCopyResults));
            OnPropertyChanged(nameof(CanClearResults));
            OnPropertyChanged(nameof(CanExport));
        }
    }
    private bool _isExporting;

    private CancellationTokenSource? _exportCancellation;

    private ICommand? _exportCommand;
    public ICommand ExportCommand => _exportCommand ??= new AsyncRelayCommand(ExecuteExportAsync, () => CanExport);

    public async Task StartScanAsync()
    {
        if (IsScanning) return;

        _scanCancellation?.Dispose();
        _scanCancellation = new CancellationTokenSource();
        var cancellationToken = _scanCancellation.Token;
        IsScanning = true;
        Status = "Expanding targets…";
        ProgressPercent = 0;

        try
        {
            var settings = Settings.Clone();
            settings.Validate();
            var expanded = await Task.Run(
                () => TargetParser.Expand(Targets, settings.ExpansionCap, cancellationToken),
                cancellationToken);

            if (expanded.Count == 0)
            {
                Status = "Ready";
                Summary = "0 targets";
                return;
            }

            _allResults.Clear();
            _resultIndexes.Clear();
            Results.Clear();
            OnPropertyChanged(nameof(TotalResultCount));
            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(CanCopyResults));
            OnPropertyChanged(nameof(CanClearResults));
            OnPropertyChanged(nameof(CanExport));
            _completed = 0;
            _respondingCount = 0;
            _pendingVisibleRefreshes = 0;
            _nextVisibleRefreshUtc = DateTime.MinValue;
            _total = expanded.Count;
            Summary = $"0 of {_total:N0} complete";
            Status = "Scanning…";

            var progress = new CallbackProgress<ScanResult>(result =>
                _dispatcher.Invoke(() => ApplyScanResult(result)));
            await _scanner.ScanAsync(expanded, settings, progress, cancellationToken);

            ProgressPercent = 100;
            Status = "Scan complete";
            UpdateCompletionSummary();
        }
        catch (OperationCanceledException)
        {
            Status = "Scan stopped";
            UpdateCompletionSummary();
        }
        catch (Exception ex)
        {
            Status = "Cannot start scan";
            Summary = ex.Message;
        }
        finally
        {
            FlushVisibleResults();
            IsScanning = false;
        }
    }

    public void SortBy(string propertyName)
    {
        if (_sortProperty == propertyName)
        {
            _sortAscending = !_sortAscending;
        }
        else
        {
            _sortProperty = propertyName;
            _sortAscending = true;
        }

        OnPropertyChanged(nameof(SortProperty));
        OnPropertyChanged(nameof(SortAscending));
        RebuildVisibleResults();
    }

    public bool ClearResults()
    {
        if (IsScanning) return false;

        _allResults.Clear();
        _resultIndexes.Clear();
        Results.Clear();
        _completed = 0;
        _total = 0;
        _respondingCount = 0;
        _pendingVisibleRefreshes = 0;
        _nextVisibleRefreshUtc = DateTime.MinValue;
        ProgressPercent = 0;
        Status = "Ready";
        Summary = "0 targets";
        OnPropertyChanged(nameof(TotalResultCount));
        OnPropertyChanged(nameof(HasResults));
        OnPropertyChanged(nameof(CanCopyResults));
        OnPropertyChanged(nameof(CanClearResults));
        OnPropertyChanged(nameof(CanExport));
        return true;
    }

    public void StopScan() => _scanCancellation?.Cancel();

    private async Task ExecuteExportAsync()
    {
        if (!CanExport) return;

        IsExporting = true;
        _exportCancellation = new CancellationTokenSource();
        var cancellationToken = _exportCancellation.Token;
        Status = "Exporting…";

        try
        {
            var snapshot = Results.ToArray();
            await ExportService.ExecuteAsync(ExportKind, ExportPath, snapshot, cancellationToken);
            Status = "Export complete";
            Summary = $"Exported {snapshot.Length:N0} result(s)";
        }
        catch (OperationCanceledException)
        {
            Status = "Export cancelled";
        }
        catch (Exception ex) when (
            ex is IOException or
            UnauthorizedAccessException or
            ArgumentException or
            InvalidOperationException or
            PlatformNotSupportedException or
            System.Runtime.InteropServices.ExternalException)
        {
            Status = "Export failed";
            Summary = ex.Message;
        }
        finally
        {
            IsExporting = false;
            _exportCancellation?.Dispose();
            _exportCancellation = null;
        }
    }

    public void CancelExport() => _exportCancellation?.Cancel();

    internal void ApplyTestResult(ScanResult result)
    {
        ApplyScanResult(result);
        FlushVisibleResults();
    }

    private void ApplyScanResult(ScanResult result)
    {
        var key = ResultKey(result);

        if (_resultIndexes.TryGetValue(key, out var existingIndex))
        {
            var previous = _allResults[existingIndex];
            _allResults[existingIndex] = result;
            if (previous.Responding != result.Responding)
                _respondingCount += result.Responding ? 1 : -1;
        }
        else
        {
            _resultIndexes[key] = _allResults.Count;
            _allResults.Add(result);
            _completed++;
            if (result.Responding) _respondingCount++;
            OnPropertyChanged(nameof(TotalResultCount));
            OnPropertyChanged(nameof(HasResults));
            OnPropertyChanged(nameof(CanCopyResults));
            OnPropertyChanged(nameof(CanClearResults));
            OnPropertyChanged(nameof(CanExport));
        }

        _pendingVisibleRefreshes++;
        if (_total < 100 ||
            (_pendingVisibleRefreshes >= 100 && DateTime.UtcNow >= _nextVisibleRefreshUtc))
        {
            FlushVisibleResults();
            _nextVisibleRefreshUtc = DateTime.UtcNow.AddMilliseconds(250);
        }
        ProgressPercent = _total == 0
            ? 0
            : _completed >= _total
                ? 100
                : Math.Min(99, (int)Math.Round(100d * _completed / _total));
        Summary = $"{_completed:N0} of {_total:N0} complete • {_respondingCount:N0} responding";
    }

    private void FlushVisibleResults()
    {
        if (_pendingVisibleRefreshes == 0) return;
        _pendingVisibleRefreshes = 0;
        RebuildVisibleResults();
    }

    private void RebuildVisibleResults()
    {
        var visible = ResultFilters.Apply(_allResults, Filter, Search).ToList();
        visible.Sort(CompareResults);
        if (!SortAscending) visible.Reverse();

        _visibleResults.ReplaceAll(visible);
        OnPropertyChanged(nameof(HasVisibleResults));
        OnPropertyChanged(nameof(CanCopyResults));
        OnPropertyChanged(nameof(CanExport));
    }

    private int CompareResults(ScanResult left, ScanResult right) => SortProperty switch
    {
        nameof(ScanResult.Target) => NetworkAddressComparer.Instance.Compare(left.Target, right.Target),
        nameof(ScanResult.Address) => NetworkAddressComparer.Instance.Compare(left.Address, right.Address),
        nameof(ScanResult.Responding) => left.Responding.CompareTo(right.Responding),
        nameof(ScanResult.RoundtripMs) => Nullable.Compare(left.RoundtripMs, right.RoundtripMs),
        nameof(ScanResult.PacketLossPercent) => left.PacketLossPercent.CompareTo(right.PacketLossPercent),
        nameof(ScanResult.Successes) => left.Successes.CompareTo(right.Successes),
        nameof(ScanResult.ReplyTtl) => Nullable.Compare(left.ReplyTtl, right.ReplyTtl),
        nameof(ScanResult.HostName) => StringComparer.OrdinalIgnoreCase.Compare(left.HostName, right.HostName),
        _ => StringComparer.OrdinalIgnoreCase.Compare(left.Status, right.Status)
    };

    private void UpdateCompletionSummary()
    {
        var responding = _allResults.Where(row => row.Responding).ToArray();
        var unavailable = _allResults.Count - responding.Length;
        var latency = responding
            .Where(row => row.RoundtripMs.HasValue)
            .Select(row => row.RoundtripMs!.Value)
            .Order()
            .ToArray();
        long? median = latency.Length == 0 ? null : latency[(latency.Length - 1) / 2];
        var targetLabel = _allResults.Count == 1 ? "1 target" : $"{_allResults.Count:N0} targets";
        Summary = median.HasValue
            ? $"{targetLabel} • {responding.Length:N0} responding • {unavailable:N0} unavailable • median {median.Value:N0} ms"
            : $"{targetLabel} • {responding.Length:N0} responding • {unavailable:N0} unavailable";
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static string ResultKey(ScanResult result) =>
        $"{result.Target.Length}:{result.Target}\0{result.Address}";

    public void Dispose()
    {
        _scanCancellation?.Cancel();
        _scanCancellation?.Dispose();
        _scanCancellation = null;
        _exportCancellation?.Cancel();
        _exportCancellation?.Dispose();
        _exportCancellation = null;
    }

    private sealed class CallbackProgress<T>(Action<T> callback) : IProgress<T>
    {
        public void Report(T value) => callback(value);
    }

    private sealed class ResettableObservableCollection<T> : ObservableCollection<T>
    {
        public void ReplaceAll(IEnumerable<T> values)
        {
            CheckReentrancy();
            Items.Clear();
            foreach (var value in values) Items.Add(value);
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}

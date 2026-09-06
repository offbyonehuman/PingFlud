using System.Windows.Input;

namespace PingFlud.Application;

public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;

    public void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        execute();
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
{
    private int _isExecuting;

    public event EventHandler? CanExecuteChanged;
    public event Action<Exception>? UnhandledException;

    public bool CanExecute(object? parameter) =>
        Volatile.Read(ref _isExecuting) == 0 && (canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        try
        {
            await ExecuteCoreAsync();
        }
        catch (Exception ex)
        {
            UnhandledException?.Invoke(ex);
        }
    }

    public Task ExecuteAsync() => CanExecute(null) ? ExecuteCoreAsync() : Task.CompletedTask;

    private async Task ExecuteCoreAsync()
    {
        if (Interlocked.CompareExchange(ref _isExecuting, 1, 0) != 0)
            return;

        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            await execute();
        }
        finally
        {
            Volatile.Write(ref _isExecuting, 0);
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

using PingFlud.Application;

namespace PingFlud.WinUI;

internal sealed class DispatcherQueueUiDispatcher(Microsoft.UI.Dispatching.DispatcherQueue dispatcherQueue) : IUiDispatcher
{
    public void Invoke(Action action)
    {
        if (dispatcherQueue.HasThreadAccess)
        {
            action();
            return;
        }

        using var completed = new ManualResetEventSlim();
        Exception? error = null;
        if (!dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
                finally
                {
                    completed.Set();
                }
            }))
        {
            throw new InvalidOperationException("The UI dispatcher is unavailable.");
        }

        completed.Wait();
        if (error is not null) throw new InvalidOperationException("The UI update failed.", error);
    }
}

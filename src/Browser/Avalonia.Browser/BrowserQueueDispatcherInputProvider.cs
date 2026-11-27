using Avalonia.Controls.Platform;

namespace Avalonia.Browser;

/// <summary>
/// Input provider of the multithreaded dispatcher. The browser main thread writes to the input queue while the
/// dispatcher thread runs, so every pending-input query drains the queue into the grouper first. That covers
/// both the loop, which checks for input before background processing and after every wake-up, and the
/// dispatcher yielding low-priority jobs to input.
/// </summary>
internal sealed class BrowserQueueDispatcherInputProvider : ManagedDispatcherImpl.IManagedDispatcherInputProvider
{
    private readonly ManualRawEventGrouperDispatchQueue _queue;

    public BrowserQueueDispatcherInputProvider(ManualRawEventGrouperDispatchQueue queue)
    {
        _queue = queue;
    }

    public bool HasInput
    {
        get
        {
            BrowserInputQueue.Drain();
            return _queue.HasJobs;
        }
    }

    public void DispatchNextInputEvent() => _queue.DispatchNext();
}

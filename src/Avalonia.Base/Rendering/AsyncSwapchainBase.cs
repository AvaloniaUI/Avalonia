using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Reactive;
using Avalonia.Rendering.Composition;

namespace Avalonia.Rendering;

/// <summary>
/// A helper class for composition-backed swapchains, should not be a public API
/// </summary>
abstract class AsyncSwapchainBase<TImage> : IAsyncDisposable where TImage : class
{
    private readonly int _queueLen;
    private readonly string _logArea;
    protected ICompositionGpuInterop Interop { get; }
    protected CompositionDrawingSurface Target { get; }
    private readonly Queue<QueueElement> _imageQueue = new();
    private readonly Queue<ValueTask> _pendingClearOperations = new();
    private PixelSize _size;
    private bool _disposed;

    struct QueueElement
    {
        public Task Present;
        public TImage Image;
    }

    public AsyncSwapchainBase(ICompositionGpuInterop interop, CompositionDrawingSurface target,
        PixelSize size, int queueLen, string logArea)
    {
        if (queueLen < 2)
            throw new ArgumentOutOfRangeException();
        _queueLen = queueLen;
        _logArea = logArea;
        Interop = interop;
        Target = target;
        Resize(size);
    }

    static bool IsBroken(QueueElement image) => image.Present.IsFaulted;
    static bool IsReady(QueueElement image) => image.Present.Status == TaskStatus.RanToCompletion;

    TImage? CleanupAndFindNextImage()
    {
        while (_imageQueue.Count > 0 && IsBroken(_imageQueue.Peek()))
            DisposeQueueItem(_imageQueue.Dequeue());
        
        if (_imageQueue.Count < _queueLen)
            return null;

        if (IsReady(_imageQueue.Peek()))
            return _imageQueue.Dequeue().Image;
        
        return null;
    }

    protected abstract TImage CreateImage(PixelSize size);
    protected abstract ValueTask DisposeImage(TImage image);
    protected abstract Task PresentImage(TImage image);
    protected abstract void BeginDraw(TImage image);
    
    private (IDisposable session, TImage image, Task presented)? BeginDraw(bool forceCreateImage)
    {
        if (_disposed)
            throw new ObjectDisposedException(null);
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var img = CleanupAndFindNextImage();
        if (img == null)
        {
            if (_imageQueue.Count < _queueLen || forceCreateImage)
                img = CreateImage(_size);
            else
            {
                return null;
            }
        }

        BeginDraw(img);
        return (Disposable.Create(() =>
        {
            var presentTask = PresentImage(img);
            // Synchronize the user-visible task
            presentTask.ContinueWith(_ =>
            {
                if (presentTask.Status == TaskStatus.Canceled)
                    tcs.SetCanceled();
                else if (presentTask.Status == TaskStatus.Faulted)
                    tcs.SetException(presentTask.Exception!);
                else
                    tcs.SetResult(0);
            });
            _imageQueue.Enqueue(new()
            {
                Present = presentTask,
                Image = img
            });
        }), img, tcs.Task);
    }

    protected (IDisposable session, TImage image, Task presented) BeginDraw() => BeginDraw(true)!.Value;

    protected (IDisposable session, TImage image, Task presented)? TryBeginDraw() => BeginDraw(false);

    protected async ValueTask<(IDisposable session, TImage image, Task presented)> BeginDrawAsync()
    {
        while (true)
        {
            var session = BeginDraw(false);
            if (session != null)
                return session.Value;
            try
            {
                await _imageQueue.Peek().Present!;
            }
            catch
            {
                // Ignore result, we just need to wait for it
            }
        }
    }

    public void Resize(PixelSize size)
    {
        if (size.Width < 1 || size.Height < 1)
            throw new ArgumentOutOfRangeException();
        if (size == _size)
            return;
        DisposeQueueItems();
        _size = size;
    }


    async ValueTask DisposeQueueItemCore(QueueElement img)
    {
        if (img.Present != null)
            try
            {
                await img.Present;
            }
            catch
            {
                // Ignore
            }

        try
        {
            await DisposeImage(img.Image);
        }
        catch (Exception e)
        {
            Logger.TryGet(LogEventLevel.Error, _logArea)
                ?.Log(this, "Unable to dispose for swapchain image: {Exception}", e);
        }
    }

    async ValueTask DisposeQueueItemsCore(List<QueueElement> images)
    {
        foreach (var img in images) 
            await DisposeQueueItemCore(img);
    }

    void DisposeQueueItem(QueueElement image)
    {
        _pendingClearOperations.Enqueue(DisposeQueueItemCore(image));
        DrainPendingClearOperations();
    }

    void DrainPendingClearOperations()
    {
        while (_pendingClearOperations.Count > 0 && _pendingClearOperations.Peek().IsCompleted)
            _pendingClearOperations.Dequeue().GetAwaiter().GetResult();
    }
    
    void DisposeQueueItems()
    {
        if (_imageQueue.Count == 0)
            return;
        
        var images = _imageQueue.ToList();
        _imageQueue.Clear();
        _pendingClearOperations.Enqueue(DisposeQueueItemsCore(images));
        DrainPendingClearOperations();
    }

    public virtual async ValueTask DisposeAsync()
    {
        _disposed = true;
        DisposeQueueItems();
        while (_pendingClearOperations.Count > 0)
            await _pendingClearOperations.Dequeue();
    }
}
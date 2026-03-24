using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Logging;
using Avalonia.Rendering;

namespace Avalonia.Vulkan;

/// <summary>
/// <see cref="IRenderTimer"/> implementation for Vulkan.
/// </summary>
public class VulkanRenderTimer : IRenderTimer
{
    private readonly object _syncLock = new();
    private Action? _waitForPresentFence;

    /// <summary>
    /// Raised when the render timer ticks to signal a new frame should be drawn.
    /// </summary>
    /// <remarks>
    /// This event can be raised on any thread; it is the responsibility of the subscriber to
    /// switch execution to the right thread.
    /// </remarks>
    public Action<TimeSpan>? Tick { get; set; }

    /// <summary>
    /// Indicates if the timer ticks on a non-UI thread
    /// </summary>
    public bool RunsInBackground => true;

    /// <summary>
    /// Default Constructor.
    /// </summary>
    public VulkanRenderTimer()
    {
        Logger.TryGet(LogEventLevel.Debug, "VulkanDynamic")?.Log(this, "VulkanRenderTimer created with fence-based VSync");

        // Create a render loop thread that waits for presentation fences
        Thread thread = new(RenderLoop)
        {
            IsBackground = RunsInBackground,
            Name = "VulkanDynamicVSync",
        };
        thread.Start();
    }

    private void RenderLoop()
    {
        Logger.TryGet(LogEventLevel.Debug, "VulkanDynamic")?.Log(this, "VSync render loop started");
        Stopwatch sw = Stopwatch.StartNew();

        CancellationTokenSource cts = new();
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            cts.Cancel();

        // Wait for Tick to be subscribed
        while(Tick == null)
            Thread.Sleep(1);

        // Bootstrap with initial tick to start rendering
        while (_waitForPresentFence == null)
        {
            Thread.Sleep(16);
            Tick?.Invoke(sw.Elapsed);
        }

        while (!cts.IsCancellationRequested)
        {
            if (_waitForPresentFence != null)
            {
                try
                {
                    // Wait for the presentation fence to be signaled
                    // This fence is signaled when GPU completes all presentation work
                    lock (_syncLock)
                    {
                        _waitForPresentFence();
                        _waitForPresentFence = null;
                    }
                }
                catch (VulkanException e)
                {
                    Logger.TryGet(LogEventLevel.Verbose, "VulkanDynamic RenderLoop")
                        ?.Log(this, $"{e.Message}");
                    if (!e.Message.Equals("vkWaitForFences returned VK_TIMEOUT", StringComparison.OrdinalIgnoreCase))
                        throw;
                    lock (_syncLock)
                        _waitForPresentFence = null;
                }
            }
            else
            {
                //rest at 120hz support
                Thread.Sleep(8);
            }

            // Fire the render tick after presentation completes or reset
            Tick?.Invoke(sw.Elapsed);
        }
    }

    /// <summary>
    /// Updates the fence for Vsync Operations.
    /// </summary>
    /// <param name="fenceWaitAction"></param>
    public void SetPresentFenceWaitAction(Action fenceWaitAction)
    {
        lock (_syncLock)
            _waitForPresentFence = fenceWaitAction;
        Logger.TryGet(LogEventLevel.Verbose, "VulkanDynamic")
            ?.Log(this, "Present fence wait action set for VSync synchronization");
    }
}

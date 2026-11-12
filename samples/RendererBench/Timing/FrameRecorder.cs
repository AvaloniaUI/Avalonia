using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RendererBench.Timing;

/// <summary>
/// Collects render thread frame timings. Frames are only kept while a measurement window is open.
/// </summary>
internal sealed class FrameRecorder
{
    public static FrameRecorder Instance { get; } = new();

    private readonly object _lock = new();
    private readonly List<FrameSample> _frames = new();
    private readonly Dictionary<long, FrameSample> _awaitingGpu = new();
    private long _nextFrameId;
    private long _windowStart;
    private long _windowEnd;
    private bool _recording;
    private long _acquireTicks;
    private long _renderAllocated;
    private MemoryWindow _memory;

    public double? StartupMs { get; private set; }

    /// <summary>Raised on the render thread when a window frame starts drawing.</summary>
    public event Action? FrameStarted;

    public long BeginFrame()
    {
        _acquireTicks = 0;
        FrameStarted?.Invoke();
        return _nextFrameId++;
    }

    /// <summary>Adds time the current frame waited for its drawable. Called on the render thread.</summary>
    public void AddAcquire(long ticks) => _acquireTicks += ticks;

    public void EndFrame(long frameId, long start, long drawEnd, long presentEnd, double? gpuMs)
    {
        StartupMs ??= (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalMilliseconds;

        // Everything the render thread allocated since the previous frame ended.
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var renderAllocated = allocated - _renderAllocated;
        _renderAllocated = allocated;

        lock (_lock)
        {
            if (!_recording)
                return;

            _memory.FootprintPeakMb = Math.Max(_memory.FootprintPeakMb, MemoryProbe.FootprintMb());
            if (MemoryProbe.GpuMemoryMb() is { } gpuMemory)
                _memory.GpuMemoryPeakMb = Math.Max(_memory.GpuMemoryPeakMb ?? 0, gpuMemory);

            var sample = new FrameSample
            {
                StartMs = Ms(start - _windowStart),
                DrawMs = Ms(drawEnd - start - _acquireTicks),
                AcquireMs = Ms(_acquireTicks),
                PresentMs = Ms(presentEnd - drawEnd),
                GpuMs = gpuMs,
                RenderAllocatedBytes = renderAllocated,
            };
            _frames.Add(sample);
            if (gpuMs is null)
                _awaitingGpu[frameId] = sample;
        }
    }

    /// <summary>
    /// Attaches a GPU time that became available after the frame ended.
    /// </summary>
    public void ReportGpu(long frameId, double gpuMs)
    {
        lock (_lock)
        {
            if (_awaitingGpu.Remove(frameId, out var sample))
                sample.GpuMs = gpuMs;
        }
    }

    public void Start()
    {
        lock (_lock)
        {
            _frames.Clear();
            _awaitingGpu.Clear();
            _windowStart = Stopwatch.GetTimestamp();
            _memory = new MemoryWindow
            {
                FootprintStartMb = MemoryProbe.FootprintMb(),
                GpuMemoryStartMb = MemoryProbe.GpuMemoryMb(),
            };
            _memory.FootprintPeakMb = _memory.FootprintStartMb;
            _memory.GpuMemoryPeakMb = _memory.GpuMemoryStartMb;
            _recording = true;
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            _windowEnd = Stopwatch.GetTimestamp();
            _memory.FootprintEndMb = MemoryProbe.FootprintMb();
            _memory.GpuMemoryEndMb = MemoryProbe.GpuMemoryMb();
            _recording = false;
        }
    }

    public (List<FrameSample> Frames, double Seconds, MemoryWindow Memory) Snapshot()
    {
        lock (_lock)
        {
            _awaitingGpu.Clear();
            return (new List<FrameSample>(_frames), Ms(_windowEnd - _windowStart) / 1000, _memory);
        }
    }

    public static double Ms(long ticks) => ticks * 1000.0 / Stopwatch.Frequency;
}

/// <summary>Process and GPU memory over the measured window, in MB.</summary>
internal struct MemoryWindow
{
    public double FootprintStartMb;
    public double FootprintEndMb;
    public double FootprintPeakMb;
    public double? GpuMemoryStartMb;
    public double? GpuMemoryEndMb;
    public double? GpuMemoryPeakMb;
}

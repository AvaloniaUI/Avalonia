using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RendererBench;

/// <summary>
/// Output of one bench process: one backend rendering one scene in one frame pacing mode.
/// </summary>
public sealed class RunResult
{
    public string Backend { get; set; } = "";
    public string Platform { get; set; } = "";
    public string Scene { get; set; } = "";
    public string Mode { get; set; } = "";
    public double Scale { get; set; }
    public int PixelWidth { get; set; }
    public int PixelHeight { get; set; }
    public double RenderScaling { get; set; }

    /// <summary>Refresh rate of the window's display, or null when the platform does not report it.</summary>
    public double? RefreshHz { get; set; }

    public string? GlVendor { get; set; }
    public string? GlRenderer { get; set; }
    public string? GlVersion { get; set; }

    /// <summary>"timer-query", "finish", "encoder-timestamps" or "none".</summary>
    public string GpuTimingMethod { get; set; } = "none";

    public double WarmupSeconds { get; set; }
    public double MeasuredSeconds { get; set; }

    /// <summary>Process start until the first frame was presented.</summary>
    public double StartupMs { get; set; }

    public double PeakRssMb { get; set; }
    public double ManagedHeapMb { get; set; }

    /// <summary>Managed bytes allocated by all threads, per frame.</summary>
    public double AllocatedBytesPerFrame { get; set; }

    /// <summary>Managed bytes allocated by the render thread, per frame.</summary>
    public double RenderThreadAllocatedBytesPerFrame { get; set; }

    /// <summary>Time the process was paused by garbage collections during the measured window.</summary>
    public double GcPauseMs { get; set; }

    /// <summary>
    /// Process footprint at the start and end of the measured window, and its
    /// peak during it. Covers native, managed and OS-counted GPU memory.
    /// </summary>
    public double FootprintStartMb { get; set; }
    public double FootprintEndMb { get; set; }
    public double FootprintPeakMb { get; set; }

    /// <summary>GPU memory allocated by the process, or null when the platform doesn't report it.</summary>
    public double? GpuMemoryStartMb { get; set; }
    public double? GpuMemoryEndMb { get; set; }
    public double? GpuMemoryPeakMb { get; set; }

    /// <summary>Scene updates on the UI thread during the measured window.</summary>
    public long SceneFrames { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }

    public List<FrameSample> Frames { get; set; } = new();
}

/// <summary>
/// Render thread timings of one presented frame. Times are milliseconds.
/// </summary>
public sealed class FrameSample
{
    /// <summary>Frame start relative to the start of the measured window.</summary>
    public double StartMs { get; set; }

    /// <summary>CreateDrawingContext until the drawing context is disposed: scene replay. Excludes AcquireMs.</summary>
    public double DrawMs { get; set; }

    /// <summary>Waiting for the window's next drawable at the start of the frame, when the platform exposes it.</summary>
    public double AcquireMs { get; set; }

    /// <summary>Drawing context disposal: flush, submit and swap.</summary>
    public double PresentMs { get; set; }

    /// <summary>Managed bytes the render thread allocated since the previous frame ended.</summary>
    public long RenderAllocatedBytes { get; set; }

    /// <summary>GPU execution time, or null when it is not known.</summary>
    public double? GpuMs { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals)]
[JsonSerializable(typeof(RunResult))]
[JsonSerializable(typeof(List<RunResult>))]
public partial class RunResultJsonContext : JsonSerializerContext
{
}

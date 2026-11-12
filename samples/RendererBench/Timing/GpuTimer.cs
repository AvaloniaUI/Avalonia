using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.OpenGL;

namespace RendererBench.Timing;

/// <summary>
/// Measures GPU time of a frame with GL_TIME_ELAPSED queries, or with glFinish when timer queries are missing.
/// All calls happen on the render thread with the context current.
/// </summary>
internal sealed unsafe class GpuTimer : IGpuTimer
{
    private const int GL_TIME_ELAPSED = 0x88BF;
    private const int GL_QUERY_RESULT = 0x8866;
    private const int GL_QUERY_RESULT_AVAILABLE = 0x8867;
    private const int GL_GPU_DISJOINT_EXT = 0x8FBB;
    private const int QueryCount = 16;

    private readonly GlInterface _gl;
    private readonly bool _checkDisjoint;
    private readonly delegate* unmanaged<int, uint*, void> _genQueries;
    private readonly delegate* unmanaged<int, uint, void> _beginQuery;
    private readonly delegate* unmanaged<int, void> _endQuery;
    private readonly delegate* unmanaged<uint, int, uint*, void> _getQueryObjectuiv;
    private readonly delegate* unmanaged<uint, int, ulong*, void> _getQueryObjectui64v;
    private readonly delegate* unmanaged<int, int*, void> _getIntegerv;
    private readonly Stack<uint> _free = new();
    private readonly Queue<(uint Query, long FrameId)> _pending = new();
    private bool _created;
    private long _activeFrame = -1;
    private uint _activeQuery;

    private GpuTimer(GlInterface gl, bool gles)
    {
        _gl = gl;
        var suffix = gles ? "EXT" : "";
        _genQueries = (delegate* unmanaged<int, uint*, void>)gl.GetProcAddress("glGenQueries" + suffix);
        _beginQuery = (delegate* unmanaged<int, uint, void>)gl.GetProcAddress("glBeginQuery" + suffix);
        _endQuery = (delegate* unmanaged<int, void>)gl.GetProcAddress("glEndQuery" + suffix);
        _getQueryObjectuiv =
            (delegate* unmanaged<uint, int, uint*, void>)gl.GetProcAddress("glGetQueryObjectuiv" + suffix);
        _getQueryObjectui64v =
            (delegate* unmanaged<uint, int, ulong*, void>)gl.GetProcAddress("glGetQueryObjectui64v" + suffix);
        _getIntegerv = (delegate* unmanaged<int, int*, void>)gl.GetProcAddress("glGetIntegerv");
        _checkDisjoint = gles;
        UsesTimerQueries = _genQueries != null && _beginQuery != null && _endQuery != null
                           && _getQueryObjectuiv != null && _getQueryObjectui64v != null;
    }

    public bool UsesTimerQueries { get; }

    public string Method => UsesTimerQueries ? "timer-query" : "finish";

    public static GpuTimer Create(IGlContext context)
    {
        var gles = context.Version.Type == GlProfileType.OpenGLES;
        var exts = context.GlInterface.ContextInfo.Extensions;
        var hasTimerQuery = gles
            ? exts.Contains("GL_EXT_disjoint_timer_query")
            : exts.Contains("GL_ARB_timer_query") || context.Version.Major > 3
              || (context.Version.Major == 3 && context.Version.Minor >= 3);
        return hasTimerQuery ? new GpuTimer(context.GlInterface, gles) : new GpuTimer(context.GlInterface);
    }

    private GpuTimer(GlInterface gl)
    {
        _gl = gl;
    }

    public void Begin(long frameId)
    {
        if (!UsesTimerQueries)
            return;

        Poll();
        if (!_created)
        {
            var ids = stackalloc uint[QueryCount];
            _genQueries(QueryCount, ids);
            for (var i = 0; i < QueryCount; i++)
                _free.Push(ids[i]);
            _created = true;
        }

        if (!_free.TryPop(out _activeQuery))
            return;
        _beginQuery(GL_TIME_ELAPSED, _activeQuery);
        _activeFrame = frameId;
    }

    /// <summary>
    /// Ends the frame. Returns the GPU time right away in glFinish mode; timer query results arrive later
    /// through <see cref="FrameRecorder.ReportGpu"/>.
    /// </summary>
    public double? End()
    {
        if (!UsesTimerQueries)
        {
            var start = Stopwatch.GetTimestamp();
            _gl.Finish();
            return FrameRecorder.Ms(Stopwatch.GetTimestamp() - start);
        }

        if (_activeFrame < 0)
            return null;
        _endQuery(GL_TIME_ELAPSED);
        _pending.Enqueue((_activeQuery, _activeFrame));
        _activeFrame = -1;
        return null;
    }

    private void Poll()
    {
        var results = new List<(long FrameId, double Ms)>();
        while (_pending.TryPeek(out var p))
        {
            uint available;
            _getQueryObjectuiv(p.Query, GL_QUERY_RESULT_AVAILABLE, &available);
            if (available == 0)
                break;
            ulong ns;
            _getQueryObjectui64v(p.Query, GL_QUERY_RESULT, &ns);
            _pending.Dequeue();
            _free.Push(p.Query);
            results.Add((p.FrameId, ns / 1_000_000.0));
        }

        if (results.Count == 0)
            return;

        if (_checkDisjoint && _getIntegerv != null)
        {
            int disjoint;
            _getIntegerv(GL_GPU_DISJOINT_EXT, &disjoint);
            if (disjoint != 0)
                return;
        }

        foreach (var r in results)
            FrameRecorder.Instance.ReportGpu(r.FrameId, r.Ms);
    }
}

/// <summary>Measures the GPU time of window frames. Called on the render thread.</summary>
internal interface IGpuTimer
{
    string Method { get; }

    void Begin(long frameId);

    /// <summary>Returns the GPU time right away, or null when it is reported later.</summary>
    double? End();
}

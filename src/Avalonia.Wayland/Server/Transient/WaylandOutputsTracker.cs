using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Wayland.Screens;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgOutputUnstableV1;

namespace Avalonia.Wayland.Server.Transient;

/// <summary>
/// Worker-thread tracker for <c>wl_output</c> globals (plus optional
/// <c>zxdg_output_v1</c> companions). Produces an immutable
/// <see cref="WaylandOutputsSnapshot"/> on every change and pushes it
/// to the UI thread
/// </summary>
internal sealed class WaylandOutputsTracker
{
    public WaylandOutputsTracker(WaylandOutputsSinkProxy? sink)
    {
        _sink = sink;
    }

    // Track outputs that haven't got their first done event
    private readonly List<Output> _pendingOutputs = new();
    public List<Output> Outputs { get; } = new();

    private readonly WaylandOutputsSinkProxy? _sink;
    private ZxdgOutputManagerV1? _xdgOutputManager;

    /// <summary>
    /// Wires the xdg-output manager once <see cref="WaylandGlobals"/> has
    /// finished its initial round-trip. Any outputs already present (in
    /// either <see cref="_pendingOutputs"/> or <see cref="Outputs"/>) get
    /// their <c>zxdg_output_v1</c> attached on the spot; outputs added
    /// later attach inside their constructor.
    /// </summary>
    public void AttachXdgOutputManager(ZxdgOutputManagerV1 manager)
    {
        if (_xdgOutputManager != null)
            return;
        _xdgOutputManager = manager;
        foreach (var o in _pendingOutputs.Concat(Outputs))
            o.AttachXdgOutput(manager);
    }

    public void AddGlobal(WaylandGlobals globals, uint name, uint version)
    {
        if (version < 2)
            return;

        var output = new Output(this, globals.Registry, name, version);
        _pendingOutputs.Add(output);
        if (_xdgOutputManager != null)
            output.AttachXdgOutput(_xdgOutputManager);

        globals.GlobalRemoved += removedName =>
        {
            // Snapshot before mutating to avoid double-handling.
            foreach (var o in _pendingOutputs.Concat(Outputs).ToList())
            {
                if (o.Name == removedName)
                {
                    _pendingOutputs.Remove(o);
                    var wasVisible = Outputs.Remove(o);
                    o.Dispose();
                    if (wasVisible) 
                        PushSnapshot();
                    break;
                }
            }
        };
    }

    /// <summary>
    /// Promotes an output to <see cref="Outputs"/> once its initial done
    /// batch has arrived (or just rebuilds the snapshot if it was already
    /// visible and just had post-init changes applied).
    /// </summary>
    internal void OnOutputDone(Output output)
    {
        var becameVisible = _pendingOutputs.Remove(output);
        if (becameVisible)
            Outputs.Add(output);
        PushSnapshot();
    }

    private void PushSnapshot()
    {
        if (_sink == null)
            return;
        var list = new List<WaylandOutputSnapshot>(Outputs.Count);
        foreach (var o in Outputs)
            list.Add(o.ToSnapshot());
        _sink.OnOutputsChanged(new WaylandOutputsSnapshot(list));
    }

    internal sealed class Output : IDisposable
    {
        public uint Name { get; }
        private readonly WaylandOutputsTracker _tracker;
        private readonly WlOutput _output;
        private ZxdgOutputV1? _xdgOutput;

        // wl_output state
        public PixelPoint ModePosition { get; private set; }
        public PixelSize ModeSize { get; private set; }
        public int Scale { get; private set; } = 1;
        public int RefreshMilliHz { get; private set; }
        public WlOutput.SubpixelEnum Subpixel { get; private set; } = WlOutput.SubpixelEnum.Unknown;
        public WlOutput.TransformEnum Transform { get; private set; } = WlOutput.TransformEnum.Normal;
        public PixelSize PhysicalSizeMm { get; private set; }
        public string? Manufacturer { get; private set; }
        public string? Model { get; private set; }
        public string? OutputName { get; private set; }
        public string? OutputDescription { get; private set; }

        // xdg_output state
        public bool HasXdgOutput { get; private set; }
        public PixelPoint XdgPosition { get; private set; }
        public PixelSize XdgSize { get; private set; }
        public string? XdgName { get; private set; }
        public string? XdgDescription { get; private set; }

        public object Id { get; } = new();

        public WlOutput WlOutput => _output;

        public Output(WaylandOutputsTracker tracker, WlRegistry registry, uint name, uint version)
        {
            Name = name;
            _tracker = tracker;
            _output = WlOutput.Bind(registry, name, Math.Min(4u, version), new WlListener(this));
        }

        public void AttachXdgOutput(ZxdgOutputManagerV1 manager)
        {
            if (_xdgOutput != null)
                return;
            // Manager bind is gated to v3+ in WaylandGlobals, so
            // wl_output.done is the unified terminator for both wl_output
            // and zxdg_output_v1 events.
            _xdgOutput = manager.GetXdgOutput(_output, new XdgListener(this));
        }

        internal void OnWlOutputDone()
        {
            _tracker.OnOutputDone(this);
        }

        public WaylandOutputSnapshot ToSnapshot()
        {
            var (pos, size) = ComputeLogical();
            return new WaylandOutputSnapshot(
                Id: Id,
                Name: XdgName ?? OutputName,
                Description: XdgDescription ?? OutputDescription,
                Manufacturer: Manufacturer,
                Model: Model,
                LogicalPosition: pos,
                LogicalSize: size,
                IntegerScale: Scale,
                RefreshRateHz: RefreshMilliHz / 1000.0,
                Subpixel: Subpixel,
                Transform: Transform,
                PhysicalSizeMm: PhysicalSizeMm);
        }

        public PixelPoint LogicalPosition => ComputeLogical().pos;
        public PixelSize LogicalSize => ComputeLogical().size;

        private (PixelPoint pos, PixelSize size) ComputeLogical()
        {
            if (HasXdgOutput)
            {
                // https://gitlab.gnome.org/GNOME/mutter/-/issues/2631
                // If integer scale > 1 and xdg_output reports the same size
                // as wl_output.mode (which is in physical pixels), mutter
                // is lying. Fall through to the mode/scale derivation.
                var mutterpt = Scale > 1 && XdgSize == ModeSize;
                if (!mutterpt)
                    return (XdgPosition, XdgSize);
            }
            // Fallback: derive logical pixels from wl_output.mode divided
            // by wl_output.scale (geometry top-left is already in
            // compositor surface coords, no division needed). Matches
            // GTK/Qt fallback when xdg_output is absent.
            return (ModePosition, new PixelSize(
                Math.Max(1, ModeSize.Width / Math.Max(1, Scale)),
                Math.Max(1, ModeSize.Height / Math.Max(1, Scale))));
        }

        public void Dispose()
        {
            if (_xdgOutput != null)
            {
                _xdgOutput.Destroy();
                _xdgOutput.Dispose();
                _xdgOutput = null;
            }
            if (_output.IsReleaseAvailable)
                _output.Release();
            else
                _output.Dispose();
        }

        private sealed class WlListener(Output p) : WlOutput.Listener
        {
            protected override void Name(WlOutput eventSender, string name) => p.OutputName = name;

            protected override void Description(WlOutput eventSender, string description) => p.OutputDescription = description;

            protected override void Done(WlOutput eventSender) => p.OnWlOutputDone();

            protected override void Geometry(WlOutput eventSender, int x, int y, int physicalWidth, int physicalHeight,
                WlOutput.SubpixelEnum subpixel, string make, string model, WlOutput.TransformEnum transform)
            {
                p.ModePosition = new PixelPoint(x, y);
                p.PhysicalSizeMm = new PixelSize(physicalWidth, physicalHeight);
                p.Subpixel = subpixel;
                p.Manufacturer = make;
                p.Model = model;
                p.Transform = transform;
            }

            protected override void Mode(WlOutput eventSender, WlOutput.ModeEnum flags, int width, int height, int refresh)
            {
                if (!flags.HasFlag(WlOutput.ModeEnum.Current))
                    return;
                p.ModeSize = new PixelSize(width, height);
                p.RefreshMilliHz = refresh;
            }

            protected override void Scale(WlOutput eventSender, int factor) => p.Scale = factor;
        }

        private sealed class XdgListener(Output p) : ZxdgOutputV1.Listener
        {
            protected override void LogicalPosition(ZxdgOutputV1 eventSender, int x, int y)
            {
                p.HasXdgOutput = true;
                p.XdgPosition = new PixelPoint(x, y);
            }

            protected override void LogicalSize(ZxdgOutputV1 eventSender, int width, int height)
            {
                p.HasXdgOutput = true;
                p.XdgSize = new PixelSize(width, height);
            }

            // zxdg_output_v1.done is deprecated since v3 (we only bind
            // v3+); wl_output.done is the unified terminator.
            protected override void Done(ZxdgOutputV1 eventSender) { }

            protected override void Name(ZxdgOutputV1 eventSender, string name) => p.XdgName = name;

            protected override void Description(ZxdgOutputV1 eventSender, string description) => p.XdgDescription = description;
        }
    }
}

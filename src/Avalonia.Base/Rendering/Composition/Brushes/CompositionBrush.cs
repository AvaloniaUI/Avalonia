using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Styling;

namespace Avalonia.Rendering.Composition;

partial class CompositionBrush : IBrush, IRelativeTransformBrush
{
    partial void InitializeDefaultsExtra()
    {
        Server.Activate();
    }
}

partial class CompositionSolidColorBrush : ISolidColorBrush
{
    internal CompositionSolidColorBrush(Compositor compositor, ServerCompositionSolidColorBrush server, Color color) : base(compositor, server)
    {
        Server = server;
        Color = color;
        InitializeDefaults();
    }
}

partial class CompositionLinearGradientBrush : ILinearGradientBrush
{
}

partial class CompositionRadialGradientBrush : IRadialGradientBrush
{
    public double Radius => RadiusX.Scalar;
}

partial class CompositionConicGradientBrush : IConicGradientBrush
{

}


public abstract partial class CompositionGradientBrush : CompositionBrush, IGradientBrush
{
    private List<IGradientStop> _gradientStops = [];
    private GradientSpreadMethod _spreadMethod;

    internal new ServerCompositionGradientBrush Server { get; }

    /// <summary>
    /// The gradient stops. Mutations of the list itself are not tracked - assign
    /// the property to ship in-place edits made after a commit. Stops created via
    /// <see cref="Compositor.CreateGradientStop(double, Media.Color)"/> stay live
    /// on the server and animate individually; other stops are snapshotted at
    /// serialization time.
    /// </summary>
    public List<IGradientStop> GradientStops
    {
        get => _gradientStops;
        set
        {
            if (ReferenceEquals(_gradientStops, value))
                return;
            _gradientStops = value;
            RegisterForSerialization();
        }
    }

    IReadOnlyList<IGradientStop> IGradientBrush.GradientStops => GradientStops;

    /// <summary>
    /// How the gradient repeats outside the stop range.
    /// </summary>
    public GradientSpreadMethod SpreadMethod
    {
        get => _spreadMethod;
        set
        {
            if (_spreadMethod == value)
                return;
            _spreadMethod = value;
            RegisterForSerialization();
        }
    }

    partial void OnRootChanged();
    partial void OnRootChanging();

    internal CompositionGradientBrush(Compositor compositor, ServerCompositionGradientBrush server) : base(compositor, server)
    {
        Server = server;
    }
    private protected override void SerializeChangesCore(BatchStreamWriter writer)
    {
        base.SerializeChangesCore(writer);
        writer.Write(SpreadMethod);
        writer.Write(GradientStops.Count);
        foreach (var stop in GradientStops)
        {
            if (stop is CompositionGradientStop comp)
                writer.WriteObject(comp.Server);
            else
                // A mutable UI-thread stop must not cross to the render thread
                // by reference; ship its current values instead.
                writer.WriteObject(stop as ImmutableGradientStop
                    ?? new ImmutableGradientStop(stop.Offset, stop.Color));
        }
    }
}

using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositionBrush : IBrush
{
    ITransform? IBrush.Transform => Transform;
}

internal partial class ServerCompositionGradientBrush : ServerCompositionBrush, IGradientBrush
{
    internal ServerCompositionGradientBrush(ServerCompositor compositor) : base(compositor)
    {

    }

    internal static CompositionProperty<List<IGradientStop>> s_IdOfGradientStopsProperty = CompositionProperty.Register<ServerCompositionGradientBrush, List<IGradientStop>>("GradientStops", obj => ((ServerCompositionGradientBrush)obj)._gradientStops, (obj, v) => ((ServerCompositionGradientBrush)obj)._gradientStops = v, null);
    private List<IGradientStop> _gradientStops = new();
    IReadOnlyList<IGradientStop> IGradientBrush.GradientStops => GradientStops;
    public List<IGradientStop> GradientStops
    {
        get
        {
            return _gradientStops;
        }

        set
        {
            var changed = false;
            if (_gradientStops != value)
            {
                OnGradientStopsChanging();
                changed = true;
            }

            SetValue(s_IdOfGradientStopsProperty, ref _gradientStops, value);
            if (changed)
                OnGradientStopsChanged();
        }
    }

    partial void OnGradientStopsChanged();
    partial void OnGradientStopsChanging();

    public GradientSpreadMethod SpreadMethod { get; private set; }

    protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
    {
        base.DeserializeChangesCore(reader, committedAt);
        SpreadMethod = reader.Read<GradientSpreadMethod>();
        var stops = new List<IGradientStop>();
        var count = reader.Read<int>();
        for (var c = 0; c < count; c++)
        {
            var read = reader.ReadObject<IGradientStop>();
            stops.Add(read);
        }
        GradientStops = stops;
    }
}

partial class ServerCompositionConicGradientBrush : IConicGradientBrush
{

}

partial class ServerCompositionLinearGradientBrush : ILinearGradientBrush
{

}

partial class ServerCompositionRadialGradientBrush : IRadialGradientBrush
{
    public double Radius => RadiusX.Scalar;
}

partial class ServerCompositionSolidColorBrush : ISolidColorBrush
{

}

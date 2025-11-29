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

internal class ServerCompositionGradientBrush : ServerCompositionBrush, IGradientBrush
{

    internal ServerCompositionGradientBrush(ServerCompositor compositor) : base(compositor)
    {

    }

    private readonly List<IGradientStop> _gradientStops = new();
    public IReadOnlyList<IGradientStop> GradientStops => _gradientStops;
    public GradientSpreadMethod SpreadMethod { get; private set; }

    protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
    {
        base.DeserializeChangesCore(reader, committedAt);
        SpreadMethod = reader.Read<GradientSpreadMethod>();
        _gradientStops.Clear();
        var count = reader.Read<int>();
        for (var c = 0; c < count; c++)
            _gradientStops.Add(reader.ReadObject<ImmutableGradientStop>());
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

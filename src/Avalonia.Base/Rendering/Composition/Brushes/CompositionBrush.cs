using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Styling;

namespace Avalonia.Rendering.Composition;

partial class CompositionBrush : IBrush
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
    internal new ServerCompositionGradientBrush Server { get; }
    public List<IGradientStop> GradientStops { get; set; } = [];
    IReadOnlyList<IGradientStop> IGradientBrush.GradientStops => GradientStops;
    public GradientSpreadMethod SpreadMethod { get; set; }
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
                writer.WriteObject(stop);
        }
    }
}

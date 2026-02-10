using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Rendering.SceneGraph;

namespace Avalonia.Controls;

class CompositionBorderVisual : CompositionDrawListVisual
{
    private CornerRadius _cornerRadius;
    private bool _cornerRadiusChanged;
    private Thickness _borderThickness;
    private bool _borderThicknessChanged;
    
    public CompositionBorderVisual(Compositor compositor, Visual visual) : base(compositor,
        new ServerBorderVisual(compositor.Server, visual), visual)
    {
    }

    public CornerRadius CornerRadius
    {
        get => _cornerRadius;
        set
        {
            if (_cornerRadius != value)
            {
                _cornerRadiusChanged = true;
                _cornerRadius = value;
                RegisterForSerialization();
            }
        }
    }

    public Thickness BorderThickness
    {
        get => _borderThickness;
        set
        {
            if (_borderThickness != value)
            {
                _borderThicknessChanged = true;
                _borderThickness = value;
                RegisterForSerialization();
            }
        }
    }

    private protected override void SerializeChangesCore(BatchStreamWriter writer)
    {
        base.SerializeChangesCore(writer);
        writer.Write(_cornerRadiusChanged);
        if (_cornerRadiusChanged)
            writer.Write(_cornerRadius);
        writer.Write(_borderThicknessChanged);
        if (_borderThicknessChanged)
            writer.Write(_borderThickness);
    }

    class ServerBorderVisual : ServerCompositionDrawListVisual
    {
        private CornerRadius _cornerRadius;
        private Thickness _borderThickness;
        public ServerBorderVisual(ServerCompositor compositor, Visual v) : base(compositor, v)
        {
        }

        protected override void RenderCore(ServerVisualRenderContext ctx, LtrbRect currentTransformedClip)
        {
            base.RenderCore(ctx, currentTransformedClip);
        }
        protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
        {
            base.DeserializeChangesCore(reader, committedAt);
            if (reader.Read<bool>())
                _cornerRadius = reader.Read<CornerRadius>();
            if (reader.Read<bool>())
                _borderThickness = reader.Read<Thickness>();
        }


        protected override void PushClipToBounds(IDrawingContextImpl canvas)
        {
            canvas.PushClip(new Rect(0, 0, Size.X, Size.Y));
        }

    }

}

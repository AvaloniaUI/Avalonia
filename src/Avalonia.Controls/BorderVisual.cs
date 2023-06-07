using System;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Rendering.SceneGraph;

namespace Avalonia.Controls;

class CompositionBorderVisual : CompositionDrawListVisual
{
    private CornerRadius _cornerRadius;
    private bool _cornerRadiusChanged;
    
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

    private protected override void SerializeChangesCore(BatchStreamWriter writer)
    {
        base.SerializeChangesCore(writer);
        writer.Write(_cornerRadiusChanged);
        if (_cornerRadiusChanged)
            writer.Write(_cornerRadius);
    }

    class ServerBorderVisual : ServerCompositionDrawListVisual
    {
        private CornerRadius _cornerRadius;
        public ServerBorderVisual(ServerCompositor compositor, Visual v) : base(compositor, v)
        {
        }

        protected override void RenderCore(CompositorDrawingContextProxy canvas, Rect currentTransformedClip)
        {
            if (ClipToBounds)
            {
                var clipRect = Root!.SnapToDevicePixels(new Rect(new Size(Size.X, Size.Y)));
                if (_cornerRadius == default)
                    canvas.PushClip(clipRect);
                else
                    canvas.PushClip(new RoundedRect(clipRect, _cornerRadius));
            }

            base.RenderCore(canvas, currentTransformedClip);
            
            if(ClipToBounds)
                canvas.PopClip();
            
        }

        protected override void DeserializeChangesCore(BatchStreamReader reader, TimeSpan committedAt)
        {
            base.DeserializeChangesCore(reader, committedAt);
            if (reader.Read<bool>())
                _cornerRadius = reader.Read<CornerRadius>();
        }

        protected override bool HandlesClipToBounds => true;
    }

}

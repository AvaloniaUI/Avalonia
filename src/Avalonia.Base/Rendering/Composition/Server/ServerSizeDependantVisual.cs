using System;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

class ServerSizeDependantVisual : ServerCompositionContainerVisual
{
    public ServerSizeDependantVisual(ServerCompositor compositor) : base(compositor)
    {
    }

    public override LtrbRect? ComputeOwnContentBounds()
    {
        if (Size.X == 0 || Size.Y == 0)
            return null;
        return new LtrbRect(0, 0, Size.X, Size.Y);
    }

    protected override void SizeChanged()
    {
        EnqueueForOwnBoundsRecompute();
        base.SizeChanged();
    }
}
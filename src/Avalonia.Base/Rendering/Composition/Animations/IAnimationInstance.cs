using System;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

namespace Avalonia.Rendering.Composition.Animations
{
    internal interface IAnimationInstance : IServerClockItem
    {
        ServerObject TargetObject { get; }
        ExpressionVariant Evaluate(TimeSpan now, ExpressionVariant currentValue);
        void Initialize(TimeSpan startedAt, ExpressionVariant startingValue, CompositionProperty property);
        void Activate();
        void Deactivate();
        void Invalidate();
    }
}

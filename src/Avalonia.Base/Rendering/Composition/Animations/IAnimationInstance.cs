using System;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

// Special license applies, see //file: src/Avalonia.Base/Rendering/Composition/License.md

namespace Avalonia.Rendering.Composition.Animations
{
    internal interface IAnimationInstance
    {
        ServerObject TargetObject { get; }
        ExpressionVariant Evaluate(TimeSpan now, ExpressionVariant currentValue);
        void Initialize(TimeSpan startedAt, ExpressionVariant startingValue, CompositionProperty property);
        void Activate();
        void Deactivate();
        void Invalidate();
    }
}
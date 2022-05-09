using System;
using Avalonia.Rendering.Composition.Expressions;

namespace Avalonia.Rendering.Composition.Animations
{
    internal interface IAnimationInstance
    {
        ExpressionVariant Evaluate(TimeSpan now, ExpressionVariant currentValue);
        void Start(TimeSpan startedAt, ExpressionVariant startingValue);
    }
}
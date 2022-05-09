// ReSharper disable CheckNamespace
using System;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Animations
{
    public class ExpressionAnimation : CompositionAnimation
    {
        private string? _expression;
        private Expression? _parsedExpression;
        
        internal ExpressionAnimation(Compositor compositor) : base(compositor)
        {
        }

        public string? Expression
        {
            get => _expression;
            set
            {
                _expression = value;
                _parsedExpression = null;
            }
        }

        private Expression ParsedExpression => _parsedExpression ??= ExpressionParser.Parse(_expression.AsSpan());

        internal override IAnimationInstance CreateInstance(
            ServerObject targetObject, ExpressionVariant? finalValue)
            => new ExpressionAnimationInstance(ParsedExpression,
                targetObject, finalValue, CreateSnapshot(true));
    }
}
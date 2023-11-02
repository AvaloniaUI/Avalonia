// ReSharper disable CheckNamespace
using System;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Animations
{
    /// <summary>
    /// A Composition Animation that uses a mathematical equation to calculate the value for an animating property every frame.
    /// </summary>
    /// <remarks>
    /// The core of ExpressionAnimations allows a developer to define a mathematical equation that can be used to calculate the value
    /// of a targeted animating property each frame.
    /// This contrasts <see cref="KeyFrameAnimation"/>s, which use an interpolator to define how the animating
    /// property changes over time. The mathematical equation can be defined using references to properties
    /// of Composition objects, mathematical functions and operators and Input.
    /// Use the <see cref="CompositionObject.StartAnimation(string , CompositionAnimation)"/> method to start the animation.
    /// </remarks>
    public sealed class ExpressionAnimation : CompositionAnimation
    {
        private string? _expression;
        private Expression? _parsedExpression;
        
        internal ExpressionAnimation(Compositor compositor) : base(compositor)
        {
        }

        /// <summary>
        /// The mathematical equation specifying how the animated value is calculated each frame.
        /// The Expression is the core of an <see cref="ExpressionAnimation"/> and represents the equation
        /// the system will use to calculate the value of the animation property each frame.
        /// The equation is set on this property in the form of a string.
        /// Although expressions can be defined by simple mathematical equations such as "2+2",
        /// the real power lies in creating mathematical relationships where the input values can change frame over frame.
        /// </summary>
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
                targetObject, finalValue, CreateSnapshot());
    }
}

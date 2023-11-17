using System;
using Avalonia.Animation;
using Avalonia.Animation.Animators;
using Avalonia.Reactive;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Utilities;

// ReSharper disable once CheckNamespace
namespace Avalonia.Media;

public class Effect : Animatable, IAffectsRender
{
    /// <summary>
    /// Marks a property as affecting the brush's visual representation.
    /// </summary>
    /// <param name="properties">The properties.</param>
    /// <remarks>
    /// After a call to this method in a brush's static constructor, any change to the
    /// property will cause the <see cref="Invalidated"/> event to be raised on the brush.
    /// </remarks>
    protected static void AffectsRender<T>(params AvaloniaProperty[] properties)
        where T : Effect
    {
        var invalidateObserver = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(
            static e => (e.Sender as T)?.RaiseInvalidated(EventArgs.Empty));

        foreach (var property in properties)
        {
            property.Changed.Subscribe(invalidateObserver);
        }
    }

    /// <summary>
    /// Raises the <see cref="Invalidated"/> event.
    /// </summary>
    /// <param name="e">The event args.</param>
    protected void RaiseInvalidated(EventArgs e) => Invalidated?.Invoke(this, e);

    /// <inheritdoc />
    public event EventHandler? Invalidated;


    static Exception ParseError(string s) => throw new ArgumentException("Unable to parse effect: " + s);
    public static IEffect Parse(string s)
    {
        var span = s.AsSpan();
        var r = new TokenParser(span);
        if (r.TryConsume("blur"))
        {
            if (!r.TryConsume('(') || !r.TryParseDouble(out var radius) || !r.TryConsume(')') || !r.IsEofWithWhitespace())
                throw ParseError(s);
            return new ImmutableBlurEffect(radius);
        }

       
        if (r.TryConsume("drop-shadow"))
        {
            if (!r.TryConsume('(') || !r.TryParseDouble(out var offsetX)
                                   || !r.TryParseDouble(out var offsetY))
                throw ParseError(s);
            double blurRadius = 0;
            var color = Colors.Black;
            if (!r.TryConsume(')'))
            {
                if (!r.TryParseDouble(out blurRadius) || blurRadius < 0)
                    throw ParseError(s);
                if (!r.TryConsume(')'))
                {
                    var endOfExpression = s.LastIndexOf(")", StringComparison.Ordinal);
                    if (endOfExpression == -1)
                        throw ParseError(s);

                    if (!new TokenParser(span.Slice(endOfExpression + 1)).IsEofWithWhitespace())
                        throw ParseError(s);

                    if (!Color.TryParse(span.Slice(r.Position, endOfExpression - r.Position).TrimEnd(), out color))
                        throw ParseError(s);
                    return new ImmutableDropShadowEffect(offsetX, offsetY, blurRadius, color, 1);
                }
            }
            if (!r.IsEofWithWhitespace())
                throw ParseError(s);
            return new ImmutableDropShadowEffect(offsetX, offsetY, blurRadius, color, 1);
        }

        throw ParseError(s);
    }

    static Effect()
    {
        EffectAnimator.EnsureRegistered();
    }

    internal Effect()
    {
        
    }
}
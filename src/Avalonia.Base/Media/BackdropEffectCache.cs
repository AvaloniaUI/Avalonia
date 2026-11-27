using System;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Media;

/// <summary>
/// Selects whether a backdrop effect keeps a snapshot of what is behind the visual, or reads it again on
/// every draw. With a null value the compositor chooses.
/// </summary>
public abstract class BackdropEffectCache
{
    private protected BackdropEffectCache()
    {
    }

    /// <summary>
    /// Parses a backdrop cache mode name, for the <c>BackdropEffectCache="Retained"</c> attribute syntax.
    /// <c>"Default"</c> gives <c>null</c> (the compositor chooses).
    /// </summary>
    public static BackdropEffectCache? Parse(string s) => s switch
    {
        "Default" => null,
        "Retained" => new RetainedBackdropEffectCache(),
        "Volatile" => new VolatileBackdropEffectCache(),
        _ => throw new ArgumentException("Unknown BackdropEffectCache: " + s)
    };

    // We currently only allow visual to be attached to one compositor at a time, so keep it simple for now
    internal abstract CompositionBackdropEffectCache GetForCompositor(Compositor c);
}

/// <summary>
/// Keeps a snapshot of what is behind the visual and updates only its changed parts.
/// </summary>
public sealed class RetainedBackdropEffectCache : BackdropEffectCache
{
    private CompositionRetainedBackdropEffectCache? _current;

    // We currently only allow visual to be attached to one compositor at a time, so keep it simple for now
    internal override CompositionBackdropEffectCache GetForCompositor(Compositor c)
    {
        if (_current?.Compositor != c)
            _current = new CompositionRetainedBackdropEffectCache(c,
                new ServerCompositionRetainedBackdropEffectCache(c.Server));

        return _current;
    }
}

/// <summary>
/// Reads what is behind the visual again on every draw.
/// </summary>
public sealed class VolatileBackdropEffectCache : BackdropEffectCache
{
    private CompositionVolatileBackdropEffectCache? _current;

    // We currently only allow visual to be attached to one compositor at a time, so keep it simple for now
    internal override CompositionBackdropEffectCache GetForCompositor(Compositor c)
    {
        if (_current?.Compositor != c)
            _current = new CompositionVolatileBackdropEffectCache(c,
                new ServerCompositionVolatileBackdropEffectCache(c.Server));

        return _current;
    }
}

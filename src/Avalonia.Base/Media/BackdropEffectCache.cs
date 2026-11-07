using System;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Media;

/// <summary>
/// Controls whether a backdrop effect's sampled result is retained in a texture or resampled every frame.
/// A null value lets the compositor decide based on the effect's sampling radius.
/// </summary>
public abstract class BackdropEffectCache
{
    private protected BackdropEffectCache()
    {
    }

    /// <summary>
    /// Parses a cache mode name, enabling the <c>BackdropEffectCache="Retained"</c> attribute syntax.
    /// <c>"Default"</c> maps to <c>null</c> (the compositor decides).
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
/// A backdrop effect cache mode that retains the sampled backdrop in a texture.
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
/// A backdrop effect cache mode that resamples the backdrop from the live target every frame.
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

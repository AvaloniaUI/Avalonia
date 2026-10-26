using System;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Media;

/// <summary>
/// Controls whether a backdrop effect's sampled result is retained in a texture or resampled every frame.
/// A null value lets the compositor decide based on the effect's sampling radius.
/// </summary>
public abstract class BackdropEffectCacheMode
{
    private protected BackdropEffectCacheMode()
    {
    }

    /// <summary>
    /// Parses a cache mode name, enabling the <c>BackdropEffectCache="Retained"</c> attribute syntax.
    /// <c>"Default"</c> maps to <c>null</c> (the compositor decides).
    /// </summary>
    public static BackdropEffectCacheMode? Parse(string s) => s switch
    {
        "Default" => null,
        "Retained" => new RetainedBackdropEffectCache(),
        "Volatile" => new VolatileBackdropEffectCache(),
        _ => throw new ArgumentException("Unknown BackdropEffectCacheMode: " + s)
    };

    // We currently only allow visual to be attached to one compositor at a time, so keep it simple for now
    internal abstract CompositionBackdropEffectCacheMode GetForCompositor(Compositor c);
}

/// <summary>
/// A backdrop effect cache mode that retains the sampled backdrop in a texture.
/// </summary>
public sealed class RetainedBackdropEffectCache : BackdropEffectCacheMode
{
    private CompositionRetainedBackdropEffectCache? _current;

    // We currently only allow visual to be attached to one compositor at a time, so keep it simple for now
    internal override CompositionBackdropEffectCacheMode GetForCompositor(Compositor c)
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
public sealed class VolatileBackdropEffectCache : BackdropEffectCacheMode
{
    private CompositionVolatileBackdropEffectCache? _current;

    // We currently only allow visual to be attached to one compositor at a time, so keep it simple for now
    internal override CompositionBackdropEffectCacheMode GetForCompositor(Compositor c)
    {
        if (_current?.Compositor != c)
            _current = new CompositionVolatileBackdropEffectCache(c,
                new ServerCompositionVolatileBackdropEffectCache(c.Server));

        return _current;
    }
}

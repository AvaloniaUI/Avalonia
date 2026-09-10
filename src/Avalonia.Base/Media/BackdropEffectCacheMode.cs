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
        "Retained" => new RetainedBackdropEffectCacheMode(),
        "Volatile" => new VolatileBackdropEffectCacheMode(),
        _ => throw new ArgumentException("Unknown BackdropEffectCacheMode: " + s)
    };

    // We currently only allow visual to be attached to one compositor at a time, so keep it simple for now
    internal abstract CompositionBackdropEffectCacheMode GetForCompositor(Compositor c);
}

/// <summary>
/// A backdrop effect cache mode that retains the sampled backdrop in a texture.
/// </summary>
public sealed class RetainedBackdropEffectCacheMode : BackdropEffectCacheMode
{
    private CompositionRetainedBackdropEffectCacheMode? _current;

    // We currently only allow visual to be attached to one compositor at a time, so keep it simple for now
    internal override CompositionBackdropEffectCacheMode GetForCompositor(Compositor c)
    {
        if (_current?.Compositor != c)
            _current = new CompositionRetainedBackdropEffectCacheMode(c,
                new ServerCompositionRetainedBackdropEffectCacheMode(c.Server));

        return _current;
    }
}

/// <summary>
/// A backdrop effect cache mode that resamples the backdrop from the live target every frame.
/// </summary>
public sealed class VolatileBackdropEffectCacheMode : BackdropEffectCacheMode
{
    private CompositionVolatileBackdropEffectCacheMode? _current;

    // We currently only allow visual to be attached to one compositor at a time, so keep it simple for now
    internal override CompositionBackdropEffectCacheMode GetForCompositor(Compositor c)
    {
        if (_current?.Compositor != c)
            _current = new CompositionVolatileBackdropEffectCacheMode(c,
                new ServerCompositionVolatileBackdropEffectCacheMode(c.Server));

        return _current;
    }
}

using System;
using System.Collections.Generic;
using NImpeller;

namespace Avalonia.Impeller;

/// <summary>
/// Textures of one image for each backend context that draws it; a texture only works with the context
/// that created it.
/// </summary>
internal sealed class ImpellerTextureCache : IDisposable
{
    private readonly List<Entry> _entries = new();

    private sealed class Entry(ImpellerBackendContext context, ImpellerTexture? texture, int version)
    {
        public ImpellerBackendContext Context { get; } = context;
        public ImpellerTexture? Texture { get; set; } = texture;
        public int Version { get; set; } = version;
    }

    /// <summary>
    /// Returns the texture of <paramref name="version"/> for <paramref name="context"/>, creating it when
    /// missing or stale.
    /// </summary>
    public ImpellerTexture? Get(ImpellerBackendContext context, int version,
        Func<ImpellerBackendContext, ImpellerTexture?> create)
    {
        lock (_entries)
        {
            // Loops rather than predicates: this runs for every bitmap draw.
            Entry? entry = null;
            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                var e = _entries[i];
                if (e.Context.IsDisposed)
                {
                    e.Texture?.Dispose();
                    _entries.RemoveAt(i);
                }
                else if (e.Context == context)
                    entry = e;
            }

            if (entry is { Texture: not null } && entry.Version == version)
                return entry.Texture;

            // Display lists hold their own references, so a texture still used by a frame stays alive.
            entry?.Texture?.Dispose();
            var texture = create(context);
            if (entry is null)
                _entries.Add(new Entry(context, texture, version));
            else
            {
                entry.Texture = texture;
                entry.Version = version;
            }
            return texture;
        }
    }

    public void Clear()
    {
        lock (_entries)
        {
            foreach (var entry in _entries)
                entry.Texture?.Dispose();
            _entries.Clear();
        }
    }

    public void Dispose() => Clear();
}

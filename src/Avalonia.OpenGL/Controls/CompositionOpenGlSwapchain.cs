using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Rendering.Composition;

namespace Avalonia.OpenGL.Controls;

internal sealed class CompositionOpenGlSwapchain : IAsyncDisposable
{
    private readonly ICompositionGlContext _context;
    private readonly CompositionDrawingSurface _surface;
    private readonly List<Entry> _entries = new();

    internal class Entry
    {
        public required ICompositionGlTexture Texture { get; init; }
        public Task? LastPresent { get; set; }
    }

    public CompositionOpenGlSwapchain(ICompositionGlContext context, CompositionDrawingSurface surface)
    {
        _context = context;
        _surface = surface;
    }

    private Entry? CleanupAndFindNextEntry(PixelSize size)
    {
        Entry? firstFound = null;
        var foundMultiple = false;

        for (var c = _entries.Count - 1; c > -1; c--)
        {
            var entry = _entries[c];
            var ready = entry.Texture.IsReadyForDraw;
            var matches = entry.Texture.Size == size;
            var broken = entry.LastPresent is { IsCompleted: true, Status: not TaskStatus.RanToCompletion };
            if (broken || (!matches && ready))
            {
                entry.Texture.DisposeAsync();
                _entries.RemoveAt(c);
            }

            if (matches && ready)
            {
                if (firstFound == null)
                    firstFound = entry;
                else
                    foundMultiple = true;
            }
        }

        // We are making sure that there was at least one texture of the same size in flight
        // Otherwise we might encounter UI thread lockups
        return foundMultiple ? firstFound : null;
    }

    public Lease BeginDraw(PixelSize size, out CompositionGlTextureInfo texture)
    {
        var entry = CleanupAndFindNextEntry(size);
        if (entry == null)
        {
            entry = new Entry { Texture = _context.CreateTexture(_surface, size) };
            _entries.Add(entry);
        }

        var lease = entry.Texture.BeginDraw();
        texture = lease.Texture;
        return new Lease(entry, lease);
    }

    /// <summary>
    /// Presents the frame on <see cref="Dispose"/>, unless it was discarded via <see cref="Discard"/>.
    /// </summary>
    public sealed class Lease : IDisposable
    {
        private readonly Entry _entry;
        private ICompositionGlTextureLease? _lease;

        internal Lease(Entry entry, ICompositionGlTextureLease lease)
        {
            _entry = entry;
            _lease = lease;
        }

        public void Discard()
        {
            var lease = _lease;
            _lease = null;
            lease?.Dispose();
        }

        public void Dispose()
        {
            var lease = _lease;
            _lease = null;
            if (lease != null)
                _entry.LastPresent = lease.PresentAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Snapshot, since awaiting the disposal can pump the dispatcher
        foreach (var entry in _entries.ToArray())
            await entry.Texture.DisposeAsync();
        _entries.Clear();
    }
}

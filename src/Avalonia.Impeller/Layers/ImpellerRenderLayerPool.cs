using System.Collections.Generic;

namespace Avalonia.Impeller;

/// <summary>Reuses texture layers by pixel size. Layers come back a frame late, once the GPU is done with them.</summary>
internal sealed class ImpellerRenderLayerPool
{
    private const int MaxIdleFrames = 120;
    private const int MaxPerSize = 8;
    private const int MaxTotal = 64;

    private readonly ImpellerBackendContext _context;
    private readonly Dictionary<long, List<Entry>> _available = new();
    private int _totalPooled;
    private int _frame;

    private struct Entry
    {
        public IImpellerOffscreenLayer Layer;
        public int Frame;
    }

    public ImpellerRenderLayerPool(ImpellerBackendContext context) => _context = context;

    private static long Key(PixelSize size) => ((long)size.Width << 32) | (uint)size.Height;

    public IImpellerOffscreenLayer Rent(PixelSize size)
    {
        if (_available.TryGetValue(Key(size), out var list) && list.Count > 0)
        {
            var entry = list[^1];
            list.RemoveAt(list.Count - 1);
            _totalPooled--;
            return entry.Layer;
        }

        return _context.CreateTextureLayer(size, new Vector(96, 96));
    }

    public void Return(IImpellerOffscreenLayer layer)
    {
        if (layer.IsCorrupted || _totalPooled >= MaxTotal)
        {
            layer.Dispose();
            return;
        }

        var key = Key(layer.PixelSize);
        if (!_available.TryGetValue(key, out var list))
            _available[key] = list = new List<Entry>();

        if (list.Count >= MaxPerSize)
        {
            layer.Dispose();
            return;
        }

        list.Add(new Entry { Layer = layer, Frame = _frame });
        _totalPooled++;
    }

    /// <summary>Ages out idle layers, once per presented frame.</summary>
    public void EndFrame()
    {
        _frame++;
        foreach (var list in _available.Values)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (_frame - list[i].Frame > MaxIdleFrames)
                {
                    list[i].Layer.Dispose();
                    list.RemoveAt(i);
                    _totalPooled--;
                }
            }
        }
    }

    public void Dispose()
    {
        foreach (var list in _available.Values)
            foreach (var entry in list)
                entry.Layer.Dispose();
        _available.Clear();
        _totalPooled = 0;
    }
}

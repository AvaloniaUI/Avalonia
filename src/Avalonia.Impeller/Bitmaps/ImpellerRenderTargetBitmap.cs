using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia;
using NImpeller;
using SkiaSharp;

namespace Avalonia.Impeller;

/// <summary>
/// A render target bitmap that records drawing, since it has no GPU context of its own. The recording is
/// rendered by the context that draws the bitmap, or on the offscreen renderer when pixels are read.
/// </summary>
internal sealed class ImpellerRenderTargetBitmap : IRenderTargetBitmapImpl, IImpellerTextureSource,
    IDrawableBitmapImpl
{
    private readonly object _lock = new();
    private readonly List<DrawingRecorder> _recordings = new();
    private readonly Dictionary<ImpellerBackendContext, (int Version, IImpellerOffscreenLayer Layer)> _layers = new();

    // Pixels from a readback, drawn under the recordings.
    private WriteableBitmapImpl? _pixels;
    private SKImage? _snapshot;
    private int _snapshotVersion;

    public ImpellerRenderTargetBitmap(PixelSize size, Vector dpi)
    {
        PixelSize = new PixelSize(Math.Max(1, size.Width), Math.Max(1, size.Height));
        Dpi = dpi;
    }

    public Vector Dpi { get; }
    public PixelSize PixelSize { get; }
    public int Version { get; private set; } = 1;

    public PixelFormat? Format => PixelFormats.Rgba8888;
    public AlphaFormat? AlphaFormat => Avalonia.Platform.AlphaFormat.Premul;

    public IDrawingContextImpl CreateDrawingContext() =>
        new DrawingRecorder(Matrix.CreateScale(Dpi.X / 96.0, Dpi.Y / 96.0), Commit);

    private void Commit(DrawingRecorder recording)
    {
        lock (_lock)
        {
            if (recording.StartsWithClear)
            {
                ReleaseRecordings();
                _pixels?.Dispose();
                _pixels = null;
            }

            if (!recording.IsEmpty)
                _recordings.Add(recording);
            Version++;
        }
    }

    private void ReleaseRecordings()
    {
        foreach (var recording in _recordings)
            recording.Release();
        _recordings.Clear();
    }

    private void Render(IDrawingContextImpl context)
    {
        if (_pixels is not null)
        {
            var bounds = new Rect(0, 0, PixelSize.Width, PixelSize.Height);
            context.DrawBitmap(_pixels, 1, bounds, bounds);
        }

        foreach (var recording in _recordings)
        {
            context.Transform = Matrix.Identity;
            recording.Replay(context);
        }
    }

    private IImpellerOffscreenLayer RenderToLayer(ImpellerBackendContext? context)
    {
        var layer = context?.CreateTextureLayer(PixelSize, new Vector(96, 96))
                    ?? new ImpellerSkiaRasterLayer(PixelSize, new Vector(96, 96));
        using (var drawingContext = layer.CreateDrawingContext())
        {
            drawingContext.Clear(default);
            Render(drawingContext);
        }
        return layer;
    }

    private SKImage RenderToImage(ImpellerBackendContext? context)
    {
        using var layer = RenderToLayer(context);
        return layer.Snapshot();
    }

    public ImpellerTexture? GetTexture(ImpellerBackendContext context)
    {
        lock (_lock)
        {
            if (_layers.TryGetValue(context, out var entry))
            {
                if (entry.Version == Version)
                    return entry.Layer.GetTexture(context);
                // The last frame may still sample it.
                context.AddFrameResources([entry.Layer]);
            }

            foreach (var stale in _layers.Keys)
                if (stale.IsDisposed)
                    _layers.Remove(stale);

            var layer = RenderToLayer(context);
            _layers[context] = (Version, layer);
            return layer.GetTexture(context);
        }
    }

    /// <summary>The bitmap's pixels, rendered on the offscreen renderer or by Skia.</summary>
    private SKImage GetSnapshot()
    {
        lock (_lock)
        {
            if (_snapshot is not null && _snapshotVersion == Version)
                return _snapshot;
            var image = ImpellerOffscreenRenderer.TryRun(RenderToImage) ?? RenderToImage(null);
            _snapshot?.Dispose();
            _snapshot = image;
            _snapshotVersion = Version;
            return image;
        }
    }

    public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKSamplingOptions samplingOptions,
        SKPaint paint) =>
        context.Canvas.DrawImage(GetSnapshot(), sourceRect, destRect, samplingOptions, paint);

    public ILockedFramebuffer Lock()
    {
        lock (_lock)
        {
            var image = GetSnapshot();
            var pixels = new WriteableBitmapImpl(PixelSize, Dpi, PixelFormats.Rgba8888,
                Avalonia.Platform.AlphaFormat.Premul);
            using (var target = pixels.Lock())
                image.ReadPixels(new SKImageInfo(PixelSize.Width, PixelSize.Height, SKColorType.Rgba8888,
                    SKAlphaType.Premul), target.Address, target.RowBytes);

            ReleaseRecordings();
            _pixels?.Dispose();
            _pixels = pixels;

            var locked = pixels.Lock();
            return new LockedFramebuffer(locked.Address, locked.Size, locked.RowBytes, locked.Dpi, locked.Format,
                locked.AlphaFormat, () =>
                {
                    locked.Dispose();
                    lock (_lock)
                        Version++;
                });
        }
    }

    public void Save(Stream stream, BitmapEncoderOptions options) =>
        Avalonia.Skia.Helpers.ImageSavingHelper.SaveImage(GetSnapshot(), stream, options);

    public void Dispose()
    {
        lock (_lock)
        {
            ReleaseRecordings();
            _pixels?.Dispose();
            _pixels = null;
            _snapshot?.Dispose();
            _snapshot = null;
            foreach (var (context, entry) in _layers)
                if (!context.IsDisposed)
                    entry.Layer.Dispose();
            _layers.Clear();
        }
    }
}

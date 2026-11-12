using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia;
using NImpeller;
using SkiaSharp;

namespace Avalonia.Impeller;

/// <summary>
/// A layer backed by GL framebuffers. Impeller clears a surface when drawing into it, but the compositor redraws
/// layers partially, so each session draws into the other framebuffer over the current contents.
/// </summary>
internal sealed class ImpellerGlRenderLayer : IImpellerOffscreenLayer, IDrawingContextLayerWithRenderContextAffinityImpl
{
    private readonly ImpellerGlBackendContext _context;
    private ImpellerGlFramebuffer? _front;
    private ImpellerGlFramebuffer? _back;
    private bool _drawing;

    public ImpellerGlRenderLayer(ImpellerGlBackendContext context, PixelSize size, Vector dpi)
    {
        _context = context;
        PixelSize = new PixelSize(Math.Max(1, size.Width), Math.Max(1, size.Height));
        Dpi = dpi;
    }

    public PixelSize PixelSize { get; }
    public Vector Dpi { get; }
    public int Version { get; private set; } = 1;

    public bool CanBlit => true;
    public bool IsCorrupted { get; private set; }

    public IDrawingContextImpl CreateDrawingContext()
    {
        if (_drawing)
            throw new InvalidOperationException("The layer is already being drawn into.");
        ObjectDisposedException.ThrowIf(IsCorrupted, this);

        var target = _back ??= new ImpellerGlFramebuffer(_context, PixelSize);
        var surface = target.CreateSurface();
        var previous = _front;
        _drawing = true;

        // GL framebuffers are bottom-up; flipping keeps the texture top-down like uploaded ones.
        var flip = new Matrix(1, 0, 0, -1, 0, PixelSize.Height);
        return new ImpellerDrawingContext(_context, surface, PixelSize, flip,
            prologue: previous is null ? null : builder => DrawContents(builder, previous.Texture),
            onFlush: () =>
            {
                surface.Dispose();
                (_front, _back) = (target, previous);
                Version++;
                _drawing = false;
            });
    }

    private static void DrawContents(ImpellerDisplayListBuilder builder, ImpellerTexture contents)
    {
        using var paint = ImpellerPaint.New()!;
        paint.SetBlendMode(ImpellerBlendMode.kImpellerBlendModeSource);
        builder.DrawTexture(contents, new ImpellerPoint(),
            ImpellerTextureSampling.kImpellerTextureSamplingNearestNeighbor, paint);
    }

    public IDrawingContextImpl CreateDrawingContext(bool useScaledDrawing) => CreateDrawingContext();

    public ImpellerTexture? GetTexture(ImpellerBackendContext context) =>
        context == _context ? _front?.Texture : null;

    public void Blit(IDrawingContextImpl context)
    {
        var dip = new Rect(0, 0, PixelSize.Width / (Dpi.X / 96.0), PixelSize.Height / (Dpi.Y / 96.0));
        context.DrawBitmap(this, 1.0, new Rect(0, 0, PixelSize.Width, PixelSize.Height), dip);
    }

    public SKImage Snapshot()
    {
        using var bitmap = new SKBitmap(new SKImageInfo(PixelSize.Width, PixelSize.Height, SKColorType.Rgba8888,
            SKAlphaType.Premul));
        if (_front is not null)
            using (_context.GlContext.EnsureCurrent())
                _front.ReadPixels(bitmap.GetPixels());
        bitmap.SetImmutable();
        return SKImage.FromBitmap(bitmap);
    }

    public void Save(Stream stream, BitmapEncoderOptions options)
    {
        using var image = Snapshot();
        Avalonia.Skia.Helpers.ImageSavingHelper.SaveImage(image, stream, options);
    }

    public bool HasRenderContextAffinity => true;

    public IBitmapImpl CreateNonAffinedSnapshot() => new ImmutableBitmap(Snapshot());

    public void Dispose()
    {
        IsCorrupted = true;
        _front?.Dispose();
        _front = null;
        _back?.Dispose();
        _back = null;
    }
}

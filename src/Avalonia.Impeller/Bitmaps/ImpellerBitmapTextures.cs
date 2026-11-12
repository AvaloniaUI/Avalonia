using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Skia;
using NImpeller;
using SkiaSharp;

namespace Avalonia.Impeller;

/// <summary>A bitmap that provides its own texture for a backend context.</summary>
internal interface IImpellerTextureSource
{
    ImpellerTexture? GetTexture(ImpellerBackendContext context);
}

/// <summary>Uploads CPU bitmaps (Avalonia.Skia's bitmap impls) to Impeller textures.</summary>
internal static class ImpellerBitmapTextures
{
    private static readonly ConditionalWeakTable<IBitmapImpl, ImpellerTextureCache> s_caches = new();

    public static ImpellerTexture? Get(IBitmapImpl bitmap, ImpellerBackendContext context)
    {
        if (bitmap is IImpellerTextureSource source)
            return source.GetTexture(context);

        var cache = s_caches.GetValue(bitmap, _ => new ImpellerTextureCache());
        return cache.Get(context, bitmap.Version, c => Upload(c, bitmap));
    }

    private static ImpellerTexture? Upload(ImpellerBackendContext context, IBitmapImpl bitmap)
    {
        switch (bitmap)
        {
            case ImmutableBitmap immutable:
                return Upload(context, immutable.Image);
            case WriteableBitmapImpl writeable:
            {
                using var snapshot = writeable.GetSnapshot();
                return Upload(context, snapshot);
            }
            case IReadableBitmapImpl readable:
            {
                using var locked = readable.Lock();
                var info = new SKImageInfo(locked.Size.Width, locked.Size.Height, locked.Format.ToSkColorType(),
                    locked.AlphaFormat.ToSkAlphaType());
                using var image = SKImage.FromPixels(info, locked.Address, locked.RowBytes);
                return image is null ? null : Upload(context, image);
            }
            default:
                return null;
        }
    }

    public static ImpellerTexture? Upload(ImpellerBackendContext context, SKImage image)
    {
        var info = new SKImageInfo(image.Width, image.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var pixels = new NativePixels(info.BytesSize);
        if (!image.ReadPixels(info, pixels.Address, info.RowBytes))
            return null;
        return Upload(context, pixels, new PixelSize(info.Width, info.Height));
    }

    /// <summary>Uploads tightly packed RGBA8888 premultiplied pixels.</summary>
    public static unsafe ImpellerTexture? Upload(ImpellerBackendContext context, IntPtr rgba, PixelSize size)
    {
        var length = size.Width * size.Height * 4;
        using var pixels = new NativePixels(length);
        Buffer.MemoryCopy((void*)rgba, (void*)pixels.Address, length, length);
        return Upload(context, pixels, size);
    }

    private static ImpellerTexture? Upload(ImpellerBackendContext context, NativePixels pixels, PixelSize size)
    {
        var descriptor = new ImpellerTextureDescriptor
        {
            Pixel_format = ImpellerPixelFormat.kImpellerPixelFormatRGBA8888,
            Size = new ImpellerISize(size.Width, size.Height),
            Mip_count = 1
        };
        // Without a release callback Impeller copies the contents.
        return context.ImpellerContext.TextureCreateWithContentsNew(descriptor, pixels, IntPtr.Zero);
    }

    private sealed unsafe class NativePixels : IImpellerUnmanagedMemory, IDisposable
    {
        private byte* _data;

        public NativePixels(int length)
        {
            Length = (ulong)length;
            _data = (byte*)NativeMemory.Alloc((nuint)Math.Max(1, length));
        }

        public byte* Data => _data;
        public ulong Length { get; }
        public IntPtr Address => (IntPtr)_data;

        public void Dispose()
        {
            NativeMemory.Free(_data);
            _data = null;
        }
    }
}

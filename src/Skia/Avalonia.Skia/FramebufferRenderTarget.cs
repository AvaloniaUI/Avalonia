using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Reactive;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia render target that renders to a framebuffer surface. No gpu acceleration available.
    /// </summary>
    internal class FramebufferRenderTarget : IRenderTarget
    {
        private SKImageInfo _currentImageInfo;
        private IntPtr _currentFramebufferAddress;
        private SKSurface? _framebufferSurface;
        private PixelFormatConversionShim? _conversionShim;
        private IDisposable? _preFramebufferCopyHandler;
        private IFramebufferRenderTarget? _renderTarget;

        /// <summary>
        /// Create new framebuffer render target using a target surface.
        /// </summary>
        /// <param name="platformSurface">Target surface.</param>
        public FramebufferRenderTarget(IFramebufferPlatformSurface platformSurface)
        {
            _renderTarget = platformSurface.CreateFramebufferRenderTarget();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _renderTarget?.Dispose();
            _renderTarget = null;
            FreeSurface();
        }

        /// <inheritdoc />
        public IDrawingContextImpl CreateDrawingContext()
        {
            if (_renderTarget == null)
                throw new ObjectDisposedException(nameof(FramebufferRenderTarget));
            
            var framebuffer = _renderTarget.Lock();
            var framebufferImageInfo = new SKImageInfo(framebuffer.Size.Width, framebuffer.Size.Height,
                framebuffer.Format.ToSkColorType(),
                framebuffer.Format == PixelFormat.Rgb565 ? SKAlphaType.Opaque : SKAlphaType.Premul);

            CreateSurface(framebufferImageInfo, framebuffer);

            var canvas = _framebufferSurface.Canvas;

            canvas.RestoreToCount(-1);
            canvas.Save();
            canvas.ResetMatrix();

            var createInfo = new DrawingContextImpl.CreateInfo
            {
                Surface = _framebufferSurface,
                Dpi = framebuffer.Dpi
            };

            return new DrawingContextImpl(createInfo, _preFramebufferCopyHandler, canvas, framebuffer);
        }

        public bool IsCorrupted => false;

        /// <summary>
        /// Check if two images info are compatible.
        /// </summary>
        /// <param name="currentImageInfo">Current.</param>
        /// <param name="desiredImageInfo">Desired.</param>
        /// <returns>True, if images are compatible.</returns>
        private static bool AreImageInfosCompatible(SKImageInfo currentImageInfo, SKImageInfo desiredImageInfo)
        {
            return currentImageInfo.Width == desiredImageInfo.Width &&
                   currentImageInfo.Height == desiredImageInfo.Height &&
                   currentImageInfo.ColorType == desiredImageInfo.ColorType;
        }

        /// <summary>
        /// Create Skia surface backed by given framebuffer.
        /// </summary>
        /// <param name="desiredImageInfo">Desired image info.</param>
        /// <param name="framebuffer">Backing framebuffer.</param>
        [MemberNotNull(nameof(_framebufferSurface))]
        private void CreateSurface(SKImageInfo desiredImageInfo, ILockedFramebuffer framebuffer)
        {
            if (_framebufferSurface != null && AreImageInfosCompatible(_currentImageInfo, desiredImageInfo) && _currentFramebufferAddress == framebuffer.Address)
            {
                return;
            }
            
            FreeSurface();
            
            _currentFramebufferAddress = framebuffer.Address;

            var surface = SKSurface.Create(desiredImageInfo, _currentFramebufferAddress, 
                framebuffer.RowBytes, new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal));

            // If surface cannot be created - try to create a compatibility shim first
            if (surface == null)
            {
                _conversionShim = new PixelFormatConversionShim(desiredImageInfo, framebuffer.Address);
                _preFramebufferCopyHandler = _conversionShim.SurfaceCopyHandler;

                surface = _conversionShim.Surface;
            }

            _framebufferSurface = surface ?? throw new Exception("Unable to create a surface for pixel format " +
                                                                 framebuffer.Format +
                                                                 " or pixel format translator");
            _currentImageInfo = desiredImageInfo;
        }

        /// <summary>
        /// Free Skia surface.
        /// </summary>
        private void FreeSurface()
        {
            _conversionShim?.Dispose();
            _conversionShim = null;
            _preFramebufferCopyHandler = null;

            _framebufferSurface?.Dispose();
            _framebufferSurface = null;
            _currentFramebufferAddress = IntPtr.Zero;
        }

        /// <summary>
        /// Converts non-compatible pixel formats using bitmap copies.
        /// </summary>
        private class PixelFormatConversionShim : IDisposable
        {
            private readonly SKBitmap _bitmap;
            private readonly SKImageInfo _destinationInfo;
            private readonly IntPtr _framebufferAddress;

            public PixelFormatConversionShim(SKImageInfo destinationInfo, IntPtr framebufferAddress)
            {
                _destinationInfo = destinationInfo;
                _framebufferAddress = framebufferAddress;

                // Create bitmap using default platform settings
                _bitmap = new SKBitmap(destinationInfo.Width, destinationInfo.Height);
                SKColorType bitmapColorType;

                if (!_bitmap.CanCopyTo(destinationInfo.ColorType))
                {
                    bitmapColorType = _bitmap.ColorType;
                    _bitmap.Dispose();

                    throw new Exception(
                        $"Unable to create pixel format shim for conversion from {bitmapColorType} to {destinationInfo.ColorType}");
                }

                Surface = SKSurface.Create(_bitmap.Info, _bitmap.GetPixels(), _bitmap.RowBytes, new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal));

                if (Surface == null)
                {
                    bitmapColorType = _bitmap.ColorType;
                    _bitmap.Dispose();

                    throw new Exception(
                        $"Unable to create pixel format shim surface for conversion from {bitmapColorType} to {destinationInfo.ColorType}");
                }

                SurfaceCopyHandler = Disposable.Create(CopySurface);
            }

            /// <summary>
            /// Skia surface.
            /// </summary>
            public SKSurface Surface { get; }

            /// <summary>
            /// Handler to start conversion via surface copy.
            /// </summary>
            public IDisposable SurfaceCopyHandler { get; }

            /// <inheritdoc />
            public void Dispose()
            {
                Surface.Dispose();
                _bitmap.Dispose();
            }

            /// <summary>
            /// Convert and copy surface to a framebuffer.
            /// </summary>
            private void CopySurface()
            {
                using (var snapshot = Surface.Snapshot())
                {
                    snapshot.ReadPixels(_destinationInfo, _framebufferAddress, _destinationInfo.RowBytes, 0, 0,
                        SKImageCachingHint.Disallow);
                }
            }
        }
    }
}

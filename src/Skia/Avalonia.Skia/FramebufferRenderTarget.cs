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
    internal class FramebufferRenderTarget : IRenderTarget2
    {
        private SKImageInfo _currentImageInfo;
        private IntPtr _currentFramebufferAddress;
        private SKSurface? _framebufferSurface;
        private PixelFormatConversionShim? _conversionShim;
        private IFramebufferPlatformSurface _platformSurface;
        private IFramebufferRenderTarget? _renderTarget;
        private IFramebufferRenderTargetWithProperties? _renderTargetWithProperties;
        private bool _hadConversionShim;

        protected SurfaceOrientation _orientation => _platformSurface is ISurfaceOrientation o ? o.Orientation : SurfaceOrientation.Normal;

        /// <summary>
        /// Create new framebuffer render target using a target surface.
        /// </summary>
        /// <param name="platformSurface">Target surface.</param>
        public FramebufferRenderTarget(IFramebufferPlatformSurface platformSurface)
        {
            _platformSurface = platformSurface;
            _renderTarget = platformSurface.CreateFramebufferRenderTarget();
            _renderTargetWithProperties = _renderTarget as IFramebufferRenderTargetWithProperties;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _renderTarget?.Dispose();
            _renderTarget = null;
            _renderTargetWithProperties = null;
            FreeSurface();
        }

        public RenderTargetProperties Properties => new()
        {
            RetainsPreviousFrameContents = !_hadConversionShim
                                           && _renderTargetWithProperties?.RetainsFrameContents == true,
            IsSuitableForDirectRendering = true
        };


        /// <inheritdoc />
        public IDrawingContextImpl CreateDrawingContext(bool scaleDrawingToDpi) =>
            CreateDrawingContextCore(scaleDrawingToDpi,   out _);

        /// <inheritdoc />
        public IDrawingContextImpl CreateDrawingContext(PixelSize expectedPixelSize,
            out RenderTargetDrawingContextProperties properties)
            => CreateDrawingContextCore(false, out properties);
        
        IDrawingContextImpl CreateDrawingContextCore(bool scaleDrawingToDpi,
            out RenderTargetDrawingContextProperties properties)
        {
            if (_renderTarget == null)
                throw new ObjectDisposedException(nameof(FramebufferRenderTarget));

            FramebufferLockProperties lockProperties = default;
            var framebuffer = _renderTargetWithProperties?.Lock(out lockProperties) ?? _renderTarget.Lock();
            var framebufferImageInfo = new SKImageInfo(framebuffer.Size.Width, framebuffer.Size.Height,
                framebuffer.Format.ToSkColorType(),
                framebuffer.Format == PixelFormat.Rgb565 ? SKAlphaType.Opaque : SKAlphaType.Premul);

            CreateSurface(framebufferImageInfo, framebuffer);
            _hadConversionShim |= _conversionShim != null;

            var canvas = _framebufferSurface.Canvas;

            canvas.RestoreToCount(-1);
            canvas.Save();
            canvas.ResetMatrix();

            var createInfo = new DrawingContextImpl.CreateInfo
            {
                Surface = _framebufferSurface,
                Dpi = framebuffer.Dpi,
                ScaleDrawingToDpi = scaleDrawingToDpi
            };

            properties = new()
            {
                PreviousFrameIsRetained = !_hadConversionShim && lockProperties.PreviousFrameIsRetained
            };
            
            return new DrawingContextImpl(createInfo, _conversionShim?.SurfaceCopyHandler, canvas, framebuffer);
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
            var orientation = _orientation;

            if (_framebufferSurface != null && AreImageInfosCompatible(_currentImageInfo, desiredImageInfo) 
                && _currentFramebufferAddress == framebuffer.Address && _conversionShim?.Orientation == orientation)
            {
                return;
            }
            
            FreeSurface();
            
            _currentFramebufferAddress = framebuffer.Address;

            // Create a surface using the framebuffer address unless we need to rotate the display
            SKSurface? surface = null;
            if (orientation == SurfaceOrientation.Normal)
            {
                surface = SKSurface.Create(desiredImageInfo, _currentFramebufferAddress,
                    framebuffer.RowBytes, new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal));
            }

            // If surface cannot be created - try to create a compatibility shim first
            if (surface == null)
            {
                _conversionShim = new PixelFormatConversionShim(desiredImageInfo, framebuffer.Address, orientation);

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
            private readonly SurfaceOrientation _orientation;
            private readonly SKImageInfo _destinationInfo;
            private readonly IntPtr _framebufferAddress;

            public PixelFormatConversionShim(SKImageInfo destinationInfo, IntPtr framebufferAddress, SurfaceOrientation orientation)
            {
                _orientation = orientation;
                _destinationInfo = destinationInfo;
                _framebufferAddress = framebufferAddress;

                // Create bitmap using default platform settings
                _bitmap = orientation switch
                {
                    SurfaceOrientation.Rotated90 => new SKBitmap(destinationInfo.Height, destinationInfo.Width),
                    SurfaceOrientation.Rotated270 => new SKBitmap(destinationInfo.Height, destinationInfo.Width),
                    _ => new SKBitmap(destinationInfo.Width, destinationInfo.Height),
                };
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
            }

            /// <summary>
            /// Skia surface.
            /// </summary>
            public SKSurface Surface { get; }

            /// <summary>
            /// Handler to start conversion via surface copy.
            /// </summary>
            public IDisposable SurfaceCopyHandler { get => Disposable.Create(CopySurface); }

            public SurfaceOrientation Orientation => _orientation;

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
                    if (Orientation != SurfaceOrientation.Normal)
                    {
                        // rotation or flipping required
                        int width;
                        int height;

                        if (Orientation == SurfaceOrientation.Rotated180)
                        {
                            width = snapshot.Width;
                            height = snapshot.Height;
                        }
                        else
                        {
                            width = snapshot.Height;
                            height = snapshot.Width;
                        }

                        // Create a new surface with swapped width and height
                        using var rotatedSurface = SKSurface.Create(new SKImageInfo(width, height));
                        var rotatedCanvas = rotatedSurface.Canvas;

                        // Apply transformation
                        rotatedCanvas.RotateDegrees(Orientation switch
                        {
                            SurfaceOrientation.Rotated90 => 90,
                            SurfaceOrientation.Rotated180 => 180,
                            SurfaceOrientation.Rotated270 => -90,
                            _ => 0
                        });
                        rotatedCanvas.Translate(Orientation switch
                        {
                            SurfaceOrientation.Rotated90 => new SKPoint(0, -width),
                            SurfaceOrientation.Rotated180 => new SKPoint(-width, -height),
                            SurfaceOrientation.Rotated270 => new SKPoint(-height, 0),
                            _ => new SKPoint(0, 0)
                        });

                        // Draw the original image onto the rotated canvas
                        rotatedCanvas.DrawImage(snapshot, 0, 0);

                        // Return the rotated image
                        using var rotateSnapshot = rotatedSurface.Snapshot();
                        rotateSnapshot.ReadPixels(_destinationInfo, _framebufferAddress, _destinationInfo.RowBytes, 0, 0,
                            SKImageCachingHint.Disallow);
                    }
                    else
                    {
                        snapshot.ReadPixels(_destinationInfo, _framebufferAddress, _destinationInfo.RowBytes, 0, 0,
                            SKImageCachingHint.Disallow);
                    }
                }
            }
        }
    }
}

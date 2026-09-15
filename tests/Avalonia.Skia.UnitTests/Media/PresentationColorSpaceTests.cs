#nullable enable

using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media
{
    public class PresentationColorSpaceTests
    {
        [Fact]
        public void Unspecified_Should_Not_Produce_A_Color_Space()
        {
            Assert.Null(PresentationColorSpace.Unspecified.ToSKColorSpace());
        }

        [Fact]
        public void Srgb_Should_Produce_An_Srgb_Color_Space()
        {
            var colorSpace = PresentationColorSpace.Srgb.ToSKColorSpace();

            Assert.NotNull(colorSpace);
            Assert.True(colorSpace!.IsSrgb);
        }

        [Fact]
        public void DisplayP3_Should_Produce_A_Wide_Gamut_Color_Space()
        {
            var colorSpace = PresentationColorSpace.DisplayP3.ToSKColorSpace();

            Assert.NotNull(colorSpace);
            Assert.False(colorSpace!.IsSrgb);
            Assert.False(colorSpace.Equals(SKColorSpace.CreateSrgb()));
        }

        [Fact]
        public void DisplayP3_Should_Use_Srgb_Transfer_Function_With_P3_Primaries()
        {
            var colorSpace = PresentationColorSpace.DisplayP3.ToSKColorSpace();

            Assert.NotNull(colorSpace);
            Assert.True(colorSpace!.GetNumericalTransferFunction(out var transferFunction));
            Assert.Equal(SKColorSpaceTransferFn.Srgb, transferFunction);
            Assert.True(colorSpace.ToColorSpaceXyz(out var xyz));
            Assert.Equal(SKColorSpaceXyz.DisplayP3, xyz);
        }

        [Fact]
        public void ToSKColorSpace_Should_Return_The_Same_Instance_Every_Time()
        {
            Assert.Same(
                PresentationColorSpace.DisplayP3.ToSKColorSpace(),
                PresentationColorSpace.DisplayP3.ToSKColorSpace());
        }

        [Fact]
        public void ToSKColorSpace_Should_Throw_For_An_Unknown_Value()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ((PresentationColorSpace)(-1)).ToSKColorSpace());
        }

        [Fact]
        public void ToSKColorSpace_Should_Throw_For_WideGamut()
        {
            // WideGamut only expresses what the application wants. A backend has to resolve it to a
            // concrete color space before reporting it, so it must never reach the renderer.
            Assert.Throws<ArgumentException>(() => PresentationColorSpace.WideGamut.ToSKColorSpace());
        }

        [Fact]
        public void ScRgb_Should_Produce_A_Linear_Color_Space()
        {
            var colorSpace = PresentationColorSpace.ScRgb.ToSKColorSpace();

            Assert.NotNull(colorSpace);
            Assert.False(colorSpace!.IsSrgb);
            Assert.True(colorSpace.GetNumericalTransferFunction(out var transferFunction));
            Assert.Equal(SKColorSpaceTransferFn.Linear, transferFunction);
        }

        [Fact]
        public void ScRgb_Should_Require_A_Float_Color_Type()
        {
            Assert.Equal(SKColorType.RgbaF16, PresentationColorSpace.ScRgb.ToSKColorType(SKColorType.Rgba8888));
        }

        [Theory]
        [InlineData(PresentationColorSpace.Unspecified)]
        [InlineData(PresentationColorSpace.Srgb)]
        [InlineData(PresentationColorSpace.DisplayP3)]
        public void Other_Color_Spaces_Should_Keep_The_Backend_Color_Type(PresentationColorSpace colorSpace)
        {
            Assert.Equal(SKColorType.Bgra8888, colorSpace.ToSKColorType(SKColorType.Bgra8888));
        }

        [Fact]
        public void Render_Target_Without_Color_Management_Should_Be_Unspecified()
        {
            Assert.Equal(PresentationColorSpace.Unspecified, new PlainRenderTarget().GetPresentationColorSpace());
        }

        [Fact]
        public void Color_Managed_Render_Target_Should_Report_Its_Own_Color_Space()
        {
            var target = new ColorManagedRenderTarget(PresentationColorSpace.DisplayP3);

            Assert.Equal(PresentationColorSpace.DisplayP3, target.GetPresentationColorSpace());
        }

        [Fact]
        public void Layer_Should_Use_The_Color_Space_Of_The_Surface_It_Is_Composited_Into()
        {
            var colorSpace = PresentationColorSpace.DisplayP3.ToSKColorSpace();

            using var target = CreateLayer(new FormatRenderSession(SKColorType.Bgra8888, colorSpace));
            using var image = target.SnapshotImage();

            Assert.NotNull(image.ColorSpace);
            Assert.True(image.ColorSpace!.Equals(colorSpace));
            Assert.Equal(SKColorType.Bgra8888, image.ColorType);
        }

        [Fact]
        public void Layer_Should_Use_The_Wider_Color_Type_Of_An_ScRgb_Surface()
        {
            var colorSpace = PresentationColorSpace.ScRgb.ToSKColorSpace();
            var colorType = PresentationColorSpace.ScRgb.ToSKColorType(SKColorType.Bgra8888);

            using var target = CreateLayer(new FormatRenderSession(colorType, colorSpace));
            using var image = target.SnapshotImage();

            Assert.Equal(SKColorType.RgbaF16, image.ColorType);
            Assert.NotNull(image.ColorSpace);
        }

        [Fact]
        public void Layer_Without_A_Session_Format_Should_Stay_Untagged()
        {
            using var target = CreateLayer(null);
            using var image = target.SnapshotImage();

            Assert.Null(image.ColorSpace);
        }

        [Fact]
        public void Layer_With_An_Explicitly_Requested_Format_Should_Keep_It()
        {
            var colorSpace = PresentationColorSpace.DisplayP3.ToSKColorSpace();

            using var target = CreateLayer(new FormatRenderSession(SKColorType.Bgra8888, colorSpace),
                PixelFormat.Bgra8888);
            using var image = target.SnapshotImage();

            Assert.Null(image.ColorSpace);
        }

        private static SurfaceRenderTarget CreateLayer(ISkiaGpuRenderSession? session, PixelFormat? format = null) =>
            new(new SurfaceRenderTarget.CreateInfo
            {
                Width = 8,
                Height = 8,
                Dpi = new Vector(96, 96),
                Format = format,
                Session = session
            });

        // Only the format is read while an intermediate surface is created, the rest of the session
        // belongs to the surface that intermediate is later composited into.
        private class FormatRenderSession : ISkiaGpuRenderSession, ISkiaGpuRenderSessionSurfaceFormat
        {
            public FormatRenderSession(SKColorType colorType, SKColorSpace? colorSpace)
            {
                ColorType = colorType;
                ColorSpace = colorSpace;
            }

            public SKColorType ColorType { get; }
            public SKColorSpace? ColorSpace { get; }

            public GRContext GrContext => throw new NotSupportedException();
            public SKSurface SkSurface => throw new NotSupportedException();
            public double ScaleFactor => throw new NotSupportedException();
            public GRSurfaceOrigin SurfaceOrigin => throw new NotSupportedException();
            public void Dispose() { }
        }

        private class PlainRenderTarget : IPlatformRenderSurfaceRenderTarget
        {
        }

        private class ColorManagedRenderTarget : IPlatformRenderSurfaceRenderTarget, IColorManagedRenderTarget
        {
            public ColorManagedRenderTarget(PresentationColorSpace colorSpace)
            {
                ColorSpace = colorSpace;
            }

            public PresentationColorSpace ColorSpace { get; }
        }
    }
}

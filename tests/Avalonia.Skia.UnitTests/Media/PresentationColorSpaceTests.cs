#nullable enable

using System;
using Avalonia.Media;
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
        public void Render_Target_Without_Color_Management_Should_Not_Produce_A_Color_Space()
        {
            Assert.Null(new PlainRenderTarget().GetPresentationColorSpace());
        }

        [Fact]
        public void Color_Managed_Render_Target_Should_Produce_Its_Own_Color_Space()
        {
            var target = new ColorManagedRenderTarget(PresentationColorSpace.DisplayP3);

            Assert.Same(PresentationColorSpace.DisplayP3.ToSKColorSpace(), target.GetPresentationColorSpace());
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

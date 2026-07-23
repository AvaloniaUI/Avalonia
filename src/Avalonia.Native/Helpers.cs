using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    internal static class Helpers
    {
        public static Point ToAvaloniaPoint (this AvnPoint pt)
        {
            return new Point(pt.X, pt.Y);
        }

        public static PixelPoint ToAvaloniaPixelPoint(this AvnPoint pt)
        {
            return new PixelPoint((int)pt.X, (int)pt.Y);
        }

        public static AvnPoint ToAvnPoint (this Point pt)
        {
            return new AvnPoint { X = pt.X, Y = pt.Y };
        }

        public static AvnPoint ToAvnPoint(this PixelPoint pt)
        {
            return new AvnPoint { X = pt.X, Y = pt.Y };
        }

        public static AvnRect ToAvnRect (this Rect rect)
        {
            return new AvnRect() { X = rect.X, Y= rect.Y, Height = rect.Height, Width = rect.Width };
        }

        public static AvnSize ToAvnSize (this Size size)
        {
            return new AvnSize { Height = size.Height, Width = size.Width };
        }

        public static AvnColorSpace ToAvnColorSpace(this PresentationColorSpace colorSpace)
        {
            return colorSpace switch
            {
                PresentationColorSpace.Srgb => AvnColorSpace.kAvnColorSpaceSrgb,
                // Display P3 is the widest gamut a CAMetalLayer can present without going to an
                // extended range pixel format, so it is what a wide gamut request resolves to.
                PresentationColorSpace.DisplayP3 or PresentationColorSpace.WideGamut =>
                    AvnColorSpace.kAvnColorSpaceDisplayP3,
                _ => AvnColorSpace.kAvnColorSpaceUnspecified
            };
        }

        public static PresentationColorSpace ToPresentationColorSpace(this AvnColorSpace colorSpace)
        {
            return colorSpace switch
            {
                AvnColorSpace.kAvnColorSpaceSrgb => PresentationColorSpace.Srgb,
                AvnColorSpace.kAvnColorSpaceDisplayP3 => PresentationColorSpace.DisplayP3,
                _ => PresentationColorSpace.Unspecified
            };
        }

        [return: NotNullIfNotNull(nameof(s))]
        public static IAvnString? ToAvnString(this string? s)
        {
            return s != null ? new AvnString(s) : null;
        }
        
        public static Size ToAvaloniaSize (this AvnSize size)
        {
            return new Size(size.Width, size.Height);
        }

        public static Rect ToAvaloniaRect (this AvnRect rect)
        {
            return new Rect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static PixelRect ToAvaloniaPixelRect(this AvnRect rect)
        {
            return new PixelRect((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
        }
    }
}

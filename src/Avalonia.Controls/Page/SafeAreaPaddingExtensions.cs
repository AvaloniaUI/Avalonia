using System;

namespace Avalonia.Controls
{
    internal static class SafeAreaPaddingExtensions
    {
        public static Thickness ApplySafeAreaPadding(this Thickness padding, Thickness safeAreaPadding)
        {
            return new Thickness(
                Math.Max(padding.Left, safeAreaPadding.Left),
                Math.Max(padding.Top, safeAreaPadding.Top),
                Math.Max(padding.Right, safeAreaPadding.Right),
                Math.Max(padding.Bottom, safeAreaPadding.Bottom));
        }

        public static Thickness GetRemainingSafeAreaPadding(this Thickness padding, Thickness safeAreaPadding)
        {
            return new Thickness(
                Math.Max(0, safeAreaPadding.Left - padding.Left),
                Math.Max(0, safeAreaPadding.Top - padding.Top),
                Math.Max(0, safeAreaPadding.Right - padding.Right),
                Math.Max(0, safeAreaPadding.Bottom - padding.Bottom));
        }
    }
}

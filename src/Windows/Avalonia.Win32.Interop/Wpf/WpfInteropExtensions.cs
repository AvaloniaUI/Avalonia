namespace Avalonia.Win32.Interop.Wpf
{
    static class WpfInteropExtensions
    {
        public static System.Windows.Point ToWpfPoint(this Point pt) => new System.Windows.Point(pt.X, pt.Y);
        public static System.Windows.Point ToWpfPoint(this PixelPoint pt) => new System.Windows.Point(pt.X, pt.Y);
        public static Point ToAvaloniaPoint(this System.Windows.Point pt) => new Point(pt.X, pt.Y);
        public static PixelPoint ToAvaloniaPixelPoint(this System.Windows.Point pt) => new PixelPoint((int)pt.X, (int)pt.Y);
        public static System.Windows.Size ToWpfSize(this Size pt) => new System.Windows.Size(pt.Width, pt.Height);
        public static Size ToAvaloniaSize(this System.Windows.Size pt) => new Size(pt.Width, pt.Height);
    }
}

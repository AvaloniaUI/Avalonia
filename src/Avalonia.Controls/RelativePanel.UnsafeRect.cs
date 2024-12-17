// Ported from https://github.com/OrgEleCho/EleCho.WpfSuite/blob/master/EleCho.WpfSuite/Panels/RelativePanel.cs

namespace Avalonia.Controls
{
    public partial class RelativePanel
    {
        private struct UnsafeRect
        {
            public double X;
            public double Y;
            public double Width;
            public double Height;

            public UnsafeRect(double x, double y, double width, double height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }
        }
    }
}

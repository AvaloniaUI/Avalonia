using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    public class BaseScreenImpl : IScreenImpl
    {
        public virtual int ScreenCount { get; }
        public virtual Screen[] AllScreens { get; }
        public virtual Screen PrimaryScreen { get; }
        
        public virtual Screen ScreenFromBounds(Rect bounds)
        {
            Screen currMaxScreen = null;
            double maxAreaSize = 0;
            for (int i = 0; i < AllScreens.Length; i++)
            {
                double left = MathUtilities.Clamp(bounds.X, AllScreens[i].Bounds.X, AllScreens[i].Bounds.X + AllScreens[i].Bounds.Width);
                double top = MathUtilities.Clamp(bounds.Y, AllScreens[i].Bounds.Y, AllScreens[i].Bounds.Y + AllScreens[i].Bounds.Height);
                double right = MathUtilities.Clamp(bounds.X + bounds.Width, AllScreens[i].Bounds.X, AllScreens[i].Bounds.X + AllScreens[i].Bounds.Width);
                double bottom = MathUtilities.Clamp(bounds.Y + bounds.Height, AllScreens[i].Bounds.Y, AllScreens[i].Bounds.Y + AllScreens[i].Bounds.Height);
                double area = (right - left) * (bottom - top);
                if (area > maxAreaSize)
                {
                    maxAreaSize = area;
                    currMaxScreen = AllScreens[i];
                }
            }

            return currMaxScreen;
        }
    }
}
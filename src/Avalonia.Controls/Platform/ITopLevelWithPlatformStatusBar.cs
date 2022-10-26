using Avalonia.Media;

namespace Avalonia.Controls.Platform
{
    public interface ITopLevelWithPlatformStatusBar
    {
        Color StatusBarColor { get; set; }

        bool? IsStatusBarVisible { get; set; }
    }
}

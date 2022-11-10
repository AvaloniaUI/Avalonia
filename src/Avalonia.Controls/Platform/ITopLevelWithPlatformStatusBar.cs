using Avalonia.Media;

namespace Avalonia.Controls.Platform
{
    [Avalonia.Metadata.Unstable]
    public interface ITopLevelWithPlatformStatusBar
    {
        StatusBarTheme? StatusBarTheme { get; set; }

        bool? IsStatusBarVisible { get; set; }
    }

    public enum StatusBarTheme
    {
        Light,
        Dark
    }
}

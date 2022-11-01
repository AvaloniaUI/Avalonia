using Avalonia.Controls.Platform;
using Avalonia.Media;

namespace Avalonia.Controls
{
    public class MobileStatusBar
    {
        /// <summary>
        /// Defines the StatusBarTheme attached property.
        /// </summary>
        public static readonly AttachedProperty<StatusBarTheme?> StatusBarThemeProperty =
            AvaloniaProperty.RegisterAttached<MobileStatusBar, UserControl, StatusBarTheme?>(
                "StatusBarTheme",
                inherits: true);

        /// <summary>
        /// Defines the IsStatusBarVisible attached property.
        /// </summary>
        public static readonly AttachedProperty<bool?> IsStatusBarVisibleProperty =
            AvaloniaProperty.RegisterAttached<MobileStatusBar, UserControl, bool?>(
                "IsStatusBarVisible",
                inherits: true);

        static MobileStatusBar()
        {
            StatusBarThemeProperty.Changed.AddClassHandler<UserControl>((view, e) =>
            {
                if (view.Parent is TopLevel tl && tl.PlatformImpl is ITopLevelWithPlatformStatusBar topLevelStatusBar)
                {
                    topLevelStatusBar.StatusBarTheme = (StatusBarTheme)e.NewValue!;
                }
            });

            IsStatusBarVisibleProperty.Changed.AddClassHandler<UserControl>((view, e) =>
            {
                if (view.Parent is TopLevel tl && tl.PlatformImpl is ITopLevelWithPlatformStatusBar topLevelStatusBar)
                {
                    topLevelStatusBar.IsStatusBarVisible = (bool)e.NewValue!;
                }
            });
        }

        /// <summary>
        /// Helper for setting the color of the platform's status bar
        /// </summary>
        /// <param name="control">The main view attached to the toplevel</param>
        /// <param name="color">The color to set</param>
        public static void SetStatusBarTheme(UserControl control, StatusBarTheme? color)
        {
            control.SetValue(StatusBarThemeProperty, color);
        }

        /// <summary>
        /// Helper for getting the color of the platform's status bar
        /// </summary>
        /// <param name="control">The main view attached to the toplevel</param>
        /// <returns>The current color of the platform's status bar</returns>
        public static StatusBarTheme? GetStatusBarTheme(UserControl control)
        {
            return control.GetValue(StatusBarThemeProperty);
        }

        /// <summary>
        /// Helper for setting the visibility of the platform's status bar
        /// </summary>
        /// <param name="control">The main view attached to the toplevel</param>
        /// <param name="visible">The status bar visible state to set</param>
        public static void SetIsStatusBarVisible(UserControl control, bool? visible)
        {
            control.SetValue(IsStatusBarVisibleProperty, visible);
        }

        /// <summary>
        /// Helper for getting the visibility of the platform's status bar
        /// </summary>
        /// <param name="control">The main view attached to the toplevel</param>
        /// <returns>The current visibility of the platform's status bar</returns>
        public static bool? GetIsStatusBarVisible(UserControl control)
        {
            return control.GetValue(IsStatusBarVisibleProperty);
        }
    }
}

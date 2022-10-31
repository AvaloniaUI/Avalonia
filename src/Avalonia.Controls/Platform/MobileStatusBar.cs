using Avalonia.Controls.Platform;
using Avalonia.Media;

namespace Avalonia.Controls
{
    public class MobileStatusBar
    {
        /// <summary>
        /// Defines the StatusBarColor attached property.
        /// </summary>
        public static readonly AttachedProperty<Color?> StatusBarColorProperty =
            AvaloniaProperty.RegisterAttached<MobileStatusBar, UserControl, Color?>(
                "StatusBarColor",
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
            StatusBarColorProperty.Changed.AddClassHandler<UserControl>((view, e) =>
            {
                if (view.Parent is TopLevel tl && tl.PlatformImpl is ITopLevelWithPlatformStatusBar topLevelStatusBar)
                {
                    topLevelStatusBar.StatusBarColor = (Color)e.NewValue!;
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
        public static void SetStatusBarColor(UserControl control, Color? color)
        {
            control.SetValue(StatusBarColorProperty, color);
        }

        /// <summary>
        /// Helper for getting the color of the platform's status bar
        /// </summary>
        /// <param name="control">The main view attached to the toplevel</param>
        /// <returns>The current color of the platform's status bar</returns>
        public static Color? GetStatusBarColor(UserControl control)
        {
            return control.GetValue(StatusBarColorProperty);
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

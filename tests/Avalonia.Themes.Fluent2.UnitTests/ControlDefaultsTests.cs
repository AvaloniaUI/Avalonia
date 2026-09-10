using Avalonia.Controls;
using Avalonia.Threading;

namespace Avalonia.Themes.Fluent2.UnitTests;

/// <summary>
/// Guards the deliberate desktop-ergonomic deviations from the WinUI metrics.
/// </summary>
public class ControlDefaultsTests
{
    [AvaloniaFact]
    public void CheckBox_and_RadioButton_do_not_force_winui_min_width()
    {
        var theme = new Fluent2Theme();
        var app = Application.Current!;
        app.Styles.Add(theme);
        try
        {
            var checkBox = new CheckBox { Content = "Ok" };
            var radioButton = new RadioButton { Content = "Ok" };
            var window = new Window
            {
                Width = 400,
                Height = 300,
                Content = new StackPanel { Children = { checkBox, radioButton } },
            };
            window.Show();
            try
            {
                Dispatcher.UIThread.RunJobs();

                // WinUI forces MinWidth=120 on both; v1 never did, and neither does Fluent2.
                Assert.Equal(0d, checkBox.MinWidth);
                Assert.Equal(0d, radioButton.MinWidth);
                Assert.True(checkBox.Bounds.Width < 120,
                    $"CheckBox width {checkBox.Bounds.Width} should hug its content.");
                Assert.True(radioButton.Bounds.Width < 120,
                    $"RadioButton width {radioButton.Bounds.Width} should hug its content.");
            }
            finally
            {
                window.Close();
            }
        }
        finally
        {
            app.Styles.Remove(theme);
        }
    }
}

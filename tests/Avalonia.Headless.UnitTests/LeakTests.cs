using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Avalonia.Headless.UnitTests;

public class LeakTests
{
    public static IEnumerable<object[]> TestData { get; } =
        Enumerable.Range(0, 50).Select(i => new object[] { i.ToString() });

    private static WeakReference s_previousFontManager;

#if NUNIT
    [TestCaseSource(nameof(TestData))]
    [AvaloniaTest]
#elif XUNIT
    [MemberData(nameof(TestData))]
    [AvaloniaTheory]
#endif
    public void Previous_FontManager_Should_Be_Collected(string data)
    {
        // Arrange
        var fontManager = new WeakReference(FontManager.Current);
        var button = new Button { Content = data };
        var window = new Window
        {
            Content = button
        };

        // Act, just some interaction, to make sure the FontManager is actually used
        window.Show();
        button.Focus();
        window.MouseDown(new Point(1,1), MouseButton.Left);
        window.Close();

        // Assert
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Either previous font manager is collected (IsAlive == false), or it is the same as current (shared isolation mode).
        if (s_previousFontManager is not null && s_previousFontManager.Target != fontManager.Target)
        {
            AssertHelper.False(s_previousFontManager.IsAlive);
        }

        s_previousFontManager = fontManager;
    }
}

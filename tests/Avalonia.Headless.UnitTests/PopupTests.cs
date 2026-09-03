using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.Headless.UnitTests;

public class PopupTests
{
#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Point_To_Screen_Respects_Window_Position()
    {
        var window = new Window { Width = 100, Height = 100 };
        window.Position = new PixelPoint(100, 200);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        AssertHelper.Equal(new PixelPoint(110, 220), window.PointToScreen(new Point(10, 20)));
        AssertHelper.Equal(new Point(10, 20), window.PointToClient(new PixelPoint(110, 220)));

        window.Close();
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Popup_Uses_Dedicated_TopLevel()
    {
        var target = new Border { Background = Brushes.Red };
        var popup = new Popup
        {
            PlacementTarget = target,
            Child = new Border { Width = 20, Height = 20 }
        };
        var window = new Window
        {
            Width = 100,
            Height = 100,
            Content = new Panel { Children = { target, popup } }
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        AssertHelper.False(popup.IsOpen);
        AssertHelper.False(popup.IsUsingOverlayLayer);
        AssertHelper.Null(GetPopupTopLevel(popup));

        popup.Open();
        Dispatcher.UIThread.RunJobs();

        AssertHelper.True(popup.IsOpen);
        AssertHelper.False(popup.IsUsingOverlayLayer);
        AssertHelper.True(GetPopupTopLevel(popup) is PopupRoot);

        window.Close();

        AssertHelper.False(popup.IsOpen);
        AssertHelper.False(popup.IsUsingOverlayLayer);
        AssertHelper.Null(GetPopupTopLevel(popup));
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Popup_Placement_Respects_Window_Position()
    {
        var target = new Border
        {
            Width = 20,
            Height = 20,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Brushes.Red
        };
        var popup = new Popup
        {
            PlacementTarget = target,
            Placement = PlacementMode.Bottom,
            Child = new Border { Width = 20, Height = 20 }
        };
        var window = new Window
        {
            Width = 100,
            Height = 100,
            Content = new Panel { Children = { target, popup } }
        };
        window.Position = new PixelPoint(100, 200);
        window.Show();
        Dispatcher.UIThread.RunJobs();

        popup.Open();
        Dispatcher.UIThread.RunJobs();

        var popupRoot = GetPopupTopLevel(popup);
        AssertHelper.NotNull(popupRoot);

        var expected = target.PointToScreen(new Point(0, target.Bounds.Height));
        AssertHelper.Equal(expected, popupRoot.PointToScreen(default));

        window.Close();
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Can_Click_Button_Inside_Platform_Popup()
    {
        var clickCount = 0;
        var button = new Button { Width = 80, Height = 30 };
        button.Click += (_, _) => clickCount++;

        var target = new Border { Background = Brushes.Red };
        var popup = new Popup { PlacementTarget = target, Child = button };
        var window = new Window
        {
            Width = 100,
            Height = 100,
            Content = new Panel { Children = { target, popup } }
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        popup.Open();
        Dispatcher.UIThread.RunJobs();

        var popupRoot = GetPopupTopLevel(popup);
        AssertHelper.NotNull(popupRoot);

        popupRoot.MouseDown(new Point(40, 15), MouseButton.Left);
        popupRoot.MouseUp(new Point(40, 15), MouseButton.Left);

        AssertHelper.Equal(1, clickCount);

        window.Close();
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Nested_Popup_Is_Owned_By_Parent_Popup()
    {
        var nestedTarget = new Border { Width = 20, Height = 20, Background = Brushes.Green };
        var nestedPopup = new Popup
        {
            PlacementTarget = nestedTarget,
            Child = new Border { Width = 10, Height = 10 }
        };
        var target = new Border { Background = Brushes.Red };
        var popup = new Popup
        {
            PlacementTarget = target,
            Child = new Panel { Children = { nestedTarget, nestedPopup } }
        };
        var window = new Window
        {
            Width = 100,
            Height = 100,
            Content = new Panel { Children = { target, popup } }
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        popup.Open();
        Dispatcher.UIThread.RunJobs();
        nestedPopup.Open();
        Dispatcher.UIThread.RunJobs();

        AssertHelper.Equal(1, window.OpenedPopups.Count);
        AssertHelper.Same(popup, window.OpenedPopups[0]);

        AssertHelper.Equal(1, popup.OpenedPopups.Count);
        AssertHelper.Same(nestedPopup, popup.OpenedPopups[0]);
        AssertHelper.Equal(0, nestedPopup.OpenedPopups.Count);

        // The nested popup is hosted in the parent popup's own top level.
        AssertHelper.Same(GetPopupTopLevel(popup), TopLevel.GetTopLevel(nestedTarget));

        nestedPopup.Close();
        Dispatcher.UIThread.RunJobs();

        AssertHelper.Equal(0, popup.OpenedPopups.Count);
        AssertHelper.Equal(1, window.OpenedPopups.Count);

        popup.Close();
        Dispatcher.UIThread.RunJobs();

        AssertHelper.Equal(0, window.OpenedPopups.Count);

        window.Close();
    }

    internal static TopLevel GetPopupTopLevel(Popup popup)
    {
        AssertHelper.NotNull(popup.Child);
        var topLevel = TopLevel.GetTopLevel(popup.Child);
        return topLevel;
    }
}

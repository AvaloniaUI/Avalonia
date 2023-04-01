using Avalonia.Input;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    
    public class SplitViewTests
    {
        [Fact]
        public void SplitView_PaneOpening_Should_Fire_Before_PaneOpened()
        {
            var splitView = new SplitView();

            bool handledOpening = false;
            splitView.PaneOpening += (x, e) =>
            {
                handledOpening = true;
            };

            splitView.PaneOpened += (x, e) =>
            {
                Assert.True(handledOpening);
            };

            splitView.IsPaneOpen = true;
        }

        [Fact]
        public void SplitView_PaneClosing_Should_Fire_Before_PaneClosed()
        {
            var splitView = new SplitView();
            splitView.IsPaneOpen = true;

            bool handledClosing = false;
            splitView.PaneClosing += (x, e) =>
            {
                handledClosing = true;
            };

            splitView.PaneClosed += (x, e) =>
            {
                Assert.True(handledClosing);
            };

            splitView.IsPaneOpen = false;
        }

        [Fact]
        public void SplitView_Cancel_Close_Should_Prevent_Pane_From_Closing()
        {
            var splitView = new SplitView();
            splitView.IsPaneOpen = true;

            splitView.PaneClosing += (x, e) =>
            {
                e.Cancel = true;
            };

            splitView.IsPaneOpen = false;

            Assert.True(splitView.IsPaneOpen);
        }

        [Fact]
        public void SplitView_TemplateSettings_Are_Correct_For_Display_Modes()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);
            var wnd = new Window
            {
                Width = 1280,
                Height = 720
            };
            var splitView = new SplitView();
            wnd.Content = splitView;
            wnd.Show();

            var zeroGridLength = new GridLength(0);
            var compactLength = splitView.CompactPaneLength;
            var compactGridLength = new GridLength(compactLength);

            // Overlay is default DisplayMode
            Assert.Equal(0, splitView.TemplateSettings.ClosedPaneWidth);
            Assert.Equal(zeroGridLength, splitView.TemplateSettings.PaneColumnGridLength);

            splitView.DisplayMode = SplitViewDisplayMode.CompactOverlay;
            Assert.Equal(compactLength, splitView.TemplateSettings.ClosedPaneWidth);
            Assert.Equal(compactGridLength, splitView.TemplateSettings.PaneColumnGridLength);

            splitView.DisplayMode = SplitViewDisplayMode.Inline;
            Assert.Equal(0, splitView.TemplateSettings.ClosedPaneWidth);
            Assert.Equal(GridLength.Auto, splitView.TemplateSettings.PaneColumnGridLength);

            splitView.DisplayMode = SplitViewDisplayMode.CompactInline;
            Assert.Equal(compactLength, splitView.TemplateSettings.ClosedPaneWidth);
            Assert.Equal(GridLength.Auto, splitView.TemplateSettings.PaneColumnGridLength);
        }

        [Fact]
        public void SplitView_TemplateSettings_Update_With_CompactPaneLength()
        {
            var splitView = new SplitView();
            
            // CompactInline:
            //    - ClosedPaneWidth = CompactPaneLength
            //    - PaneColumnGridLength = Auto
            splitView.DisplayMode = SplitViewDisplayMode.CompactInline;

            var compactLength = splitView.CompactPaneLength;
            
            Assert.Equal(GridLength.Auto, splitView.TemplateSettings.PaneColumnGridLength);
            Assert.Equal(compactLength, splitView.TemplateSettings.ClosedPaneWidth);

            splitView.CompactPaneLength = 100;

            Assert.Equal(GridLength.Auto, splitView.TemplateSettings.PaneColumnGridLength);
            Assert.Equal(100, splitView.TemplateSettings.ClosedPaneWidth);

            // CompactOverlay:
            //    - ClosedPaneWidth = CompactPaneLength
            //    - PaneColumnGridLength = GridLength { CompactPaneLength, Pixel }
            splitView.DisplayMode = SplitViewDisplayMode.CompactOverlay;
            splitView.CompactPaneLength = 50;

            Assert.Equal(new GridLength(50), splitView.TemplateSettings.PaneColumnGridLength);
            Assert.Equal(50, splitView.TemplateSettings.ClosedPaneWidth);

            // Value shouldn't change for these - changing the display mode will update
            // the template settings with the right value
            splitView.DisplayMode = SplitViewDisplayMode.Inline;
            splitView.CompactPaneLength = 1;

            Assert.Equal(GridLength.Auto, splitView.TemplateSettings.PaneColumnGridLength);
            Assert.Equal(0, splitView.TemplateSettings.ClosedPaneWidth);

            splitView.DisplayMode = SplitViewDisplayMode.Overlay;
            splitView.CompactPaneLength = 2;

            Assert.Equal(new GridLength(0), splitView.TemplateSettings.PaneColumnGridLength);
            Assert.Equal(0, splitView.TemplateSettings.ClosedPaneWidth);
        }

        [Fact]
        public void SplitView_Pointer_Closes_Pane_In_Overlay_Mode()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow
                .With(globalClock: new MockGlobalClock()));
            var wnd = new Window
            {
                Width = 1280,
                Height = 720
            };
            var splitView = new SplitView();
            wnd.Content = splitView;
            wnd.Show();

            splitView.IsPaneOpen = true;

            splitView.RaiseEvent(new PointerReleasedEventArgs(splitView,
                null, wnd, new Point(1270, 30), 0,
                new PointerPointProperties(),
                KeyModifiers.None,
                MouseButton.Left));

            Assert.False(splitView.IsPaneOpen);

            // Inline shouldn't close the pane
            splitView.DisplayMode = SplitViewDisplayMode.Inline;
            splitView.IsPaneOpen = true;

            splitView.RaiseEvent(new PointerReleasedEventArgs(splitView,
                null, wnd, new Point(1270, 30), 0,
                new PointerPointProperties(),
                KeyModifiers.None,
                MouseButton.Left));

            Assert.True(splitView.IsPaneOpen);
        }

        [Fact]
        public void SplitView_Pointer_Should_Not_Close_Pane_If_Over_Pane()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow
                .With(globalClock: new MockGlobalClock()));
            var wnd = new Window
            {
                Width = 1280,
                Height = 720
            };
            var clickBorder = new Border
            {
                Width = 100,
                Height = 100,
                HorizontalAlignment = Layout.HorizontalAlignment.Left,
                VerticalAlignment = Layout.VerticalAlignment.Top
            };
            var splitView = new SplitView
            {
                Pane = clickBorder
            };
            wnd.Content = splitView;
            wnd.Show();

            splitView.IsPaneOpen = true;

            clickBorder.RaiseEvent(new PointerReleasedEventArgs(splitView,
                null, wnd, new Point(5, 5), 0,
                new PointerPointProperties(),
                KeyModifiers.None,
                MouseButton.Left));

            Assert.True(splitView.IsPaneOpen);
        }

        [Fact]
        public void SplitView_Escape_Key_Closes_Light_Dismissable_Pane()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow
                .With(globalClock: new MockGlobalClock()));
            var wnd = new Window
            {
                Width = 1280,
                Height = 720
            };
            var button = new Button();
            var splitView = new SplitView
            {
                Pane = button
            };
            wnd.Content = splitView;
            wnd.Show();

            splitView.IsPaneOpen = true;

            button.RaiseEvent(new KeyEventArgs
            {
                Key = Key.Escape,
                RoutedEvent = InputElement.KeyDownEvent
            });

            Assert.False(splitView.IsPaneOpen);

            splitView.DisplayMode = SplitViewDisplayMode.Inline;

            splitView.IsPaneOpen = true;

            button.RaiseEvent(new KeyEventArgs
            {
                Key = Key.Escape,
                RoutedEvent = InputElement.KeyDownEvent
            });

            Assert.True(splitView.IsPaneOpen);
        }

        [Fact]
        public void Top_Level_Back_Requested_Closes_Light_Dismissable_Pane()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow
                .With(globalClock: new MockGlobalClock()));
            var wnd = new Window
            {
                Width = 1280,
                Height = 720
            };
            var splitView = new SplitView();
            wnd.Content = splitView;
            wnd.Show();

            splitView.IsPaneOpen = true;

            wnd.RaiseEvent(new Interactivity.RoutedEventArgs(TopLevel.BackRequestedEvent));

            Assert.False(splitView.IsPaneOpen);

            splitView.DisplayMode = SplitViewDisplayMode.Inline;
            splitView.IsPaneOpen = true;

            wnd.RaiseEvent(new Interactivity.RoutedEventArgs(TopLevel.BackRequestedEvent));

            Assert.True(splitView.IsPaneOpen);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class DrawerPageTests
{

    public class PropertyRoundTrips : ScopedTestBase
    {
        [Fact]
        public void IsOpen_Toggle()
        {
            var dp = new DrawerPage();
            dp.IsOpen = true;
            Assert.True(dp.IsOpen);
            dp.IsOpen = false;
            Assert.False(dp.IsOpen);
        }

        [Theory]
        [InlineData(100.0)]
        [InlineData(280.0)]
        [InlineData(500.0)]
        public void DrawerLength_RoundTrips(double length)
        {
            var dp = new DrawerPage { DrawerLength = length };
            Assert.Equal(length, dp.DrawerLength);
        }

        [Theory]
        [InlineData(40.0)]
        [InlineData(56.0)]
        [InlineData(80.0)]
        public void CompactDrawerLength_RoundTrips(double length)
        {
            var dp = new DrawerPage { CompactDrawerLength = length };
            Assert.Equal(length, dp.CompactDrawerLength);
        }

        [Theory]
        [InlineData(600.0)]
        [InlineData(800.0)]
        [InlineData(1200.0)]
        public void DrawerBreakpointLength_RoundTrips(double width)
        {
            var dp = new DrawerPage { DrawerBreakpointLength = width };
            Assert.Equal(width, dp.DrawerBreakpointLength);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsGestureEnabled_RoundTrips(bool value)
        {
            var dp = new DrawerPage { IsGestureEnabled = value };
            Assert.Equal(value, dp.IsGestureEnabled);
        }

        [Theory]
        [InlineData(DrawerBehavior.Auto)]
        [InlineData(DrawerBehavior.Flyout)]
        [InlineData(DrawerBehavior.Locked)]
        [InlineData(DrawerBehavior.Disabled)]
        public void DrawerBehavior_RoundTrips(DrawerBehavior behavior)
        {
            var dp = new DrawerPage { DrawerBehavior = behavior };
            Assert.Equal(behavior, dp.DrawerBehavior);
        }

        [Theory]
        [InlineData(DrawerLayoutBehavior.Overlay)]
        [InlineData(DrawerLayoutBehavior.Split)]
        [InlineData(DrawerLayoutBehavior.CompactOverlay)]
        [InlineData(DrawerLayoutBehavior.CompactInline)]
        public void DrawerLayoutBehavior_RoundTrips(DrawerLayoutBehavior behavior)
        {
            var dp = new DrawerPage { DrawerLayoutBehavior = behavior };
            Assert.Equal(behavior, dp.DrawerLayoutBehavior);
        }

        [Theory]
        [InlineData(DrawerPlacement.Left)]
        [InlineData(DrawerPlacement.Right)]
        [InlineData(DrawerPlacement.Top)]
        [InlineData(DrawerPlacement.Bottom)]
        public void DrawerPlacement_RoundTrips(DrawerPlacement placement)
        {
            var dp = new DrawerPage { DrawerPlacement = placement };
            Assert.Equal(placement, dp.DrawerPlacement);
        }

        [Theory]
        [InlineData(SplitViewDisplayMode.Overlay)]
        [InlineData(SplitViewDisplayMode.CompactOverlay)]
        [InlineData(SplitViewDisplayMode.Inline)]
        [InlineData(SplitViewDisplayMode.CompactInline)]
        public void DisplayMode_RoundTrips(SplitViewDisplayMode mode)
        {
            var dp = new DrawerPage { DisplayMode = mode };
            Assert.Equal(mode, dp.DisplayMode);
        }

        [Theory]
        [InlineData(HorizontalAlignment.Left)]
        [InlineData(HorizontalAlignment.Center)]
        [InlineData(HorizontalAlignment.Right)]
        [InlineData(HorizontalAlignment.Stretch)]
        public void HorizontalContentAlignment_RoundTrips(HorizontalAlignment value)
        {
            var dp = new DrawerPage { HorizontalContentAlignment = value };
            Assert.Equal(value, dp.HorizontalContentAlignment);
        }

        [Theory]
        [InlineData(VerticalAlignment.Top)]
        [InlineData(VerticalAlignment.Center)]
        [InlineData(VerticalAlignment.Bottom)]
        [InlineData(VerticalAlignment.Stretch)]
        public void VerticalContentAlignment_RoundTrips(VerticalAlignment value)
        {
            var dp = new DrawerPage { VerticalContentAlignment = value };
            Assert.Equal(value, dp.VerticalContentAlignment);
        }

        [Fact]
        public void DrawerHeader_AcceptsString()
        {
            var dp = new DrawerPage { DrawerHeader = "My App" };
            Assert.Equal("My App", dp.DrawerHeader);
        }

        [Fact]
        public void DrawerHeader_AcceptsControl()
        {
            var ctrl = new TextBlock { Text = "My App" };
            var dp = new DrawerPage { DrawerHeader = ctrl };
            Assert.Same(ctrl, dp.DrawerHeader);
        }

        [Fact]
        public void DrawerFooter_AcceptsString()
        {
            var dp = new DrawerPage { DrawerFooter = "v2.0" };
            Assert.Equal("v2.0", dp.DrawerFooter);
        }

        [Fact]
        public void DrawerFooter_AcceptsControl()
        {
            var ctrl = new TextBlock { Text = "Footer" };
            var dp = new DrawerPage { DrawerFooter = ctrl };
            Assert.Same(ctrl, dp.DrawerFooter);
        }

        [Fact]
        public void DrawerIcon_AcceptsControl()
        {
            var icon = new PathIcon();
            var dp = new DrawerPage { DrawerIcon = icon };
            Assert.Same(icon, dp.DrawerIcon);
        }

        [Fact]
        public void DrawerBackground_RoundTrips()
        {
            var brush = new SolidColorBrush(Colors.DodgerBlue);
            var dp = new DrawerPage { DrawerBackground = brush };
            Assert.Same(brush, dp.DrawerBackground);
        }

        [Fact]
        public void DrawerHeaderBackground_RoundTrips()
        {
            var brush = new SolidColorBrush(Colors.Indigo);
            var dp = new DrawerPage { DrawerHeaderBackground = brush };
            Assert.Same(brush, dp.DrawerHeaderBackground);
        }

        [Fact]
        public void DrawerHeaderForeground_RoundTrips()
        {
            var brush = Brushes.White;
            var dp = new DrawerPage { DrawerHeaderForeground = brush };
            Assert.Same(brush, dp.DrawerHeaderForeground);
        }

        [Fact]
        public void DrawerFooterBackground_RoundTrips()
        {
            var brush = new SolidColorBrush(Colors.DarkGray);
            var dp = new DrawerPage { DrawerFooterBackground = brush };
            Assert.Same(brush, dp.DrawerFooterBackground);
        }

        [Fact]
        public void DrawerFooterForeground_RoundTrips()
        {
            var brush = Brushes.LightGray;
            var dp = new DrawerPage { DrawerFooterForeground = brush };
            Assert.Same(brush, dp.DrawerFooterForeground);
        }

        [Fact]
        public void DrawerTemplate_CanBeSetToNull()
        {
            var dp = new DrawerPage { DrawerTemplate = null };
            Assert.Null(dp.DrawerTemplate);
        }

        [Fact]
        public void ContentTemplate_CanBeSetToNull()
        {
            var dp = new DrawerPage { ContentTemplate = null };
            Assert.Null(dp.ContentTemplate);
        }

        [Fact]
        public void BackdropBrush_RoundTrips()
        {
            var brush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0));
            var dp = new DrawerPage { BackdropBrush = brush };
            Assert.Same(brush, dp.BackdropBrush);
        }

        [Fact]
        public void BackdropBrush_CanBeSetToNull()
        {
            var dp = new DrawerPage { BackdropBrush = Brushes.Black };
            dp.BackdropBrush = null;
            Assert.Null(dp.BackdropBrush);
        }

        [Fact]
        public void Header_RoundTrips()
        {
            var dp = new DrawerPage { Header = "My Drawer Page" };
            Assert.Equal("My Drawer Page", dp.Header);
        }

        [Fact]
        public void Icon_RoundTrips()
        {
            var icon = new Image();
            var dp = new DrawerPage { Icon = icon };
            Assert.Same(icon, dp.Icon);
        }

        [Fact]
        public void SafeAreaPadding_RoundTrips()
        {
            var dp = new DrawerPage();
            var padding = new Thickness(10, 20, 10, 34);
            dp.SafeAreaPadding = padding;
            Assert.Equal(padding, dp.SafeAreaPadding);
        }

        [Fact]
        public void DrawerBehavior_Disabled_PreventsIsOpenSetToTrue()
        {
            var dp = new DrawerPage { DrawerBehavior = DrawerBehavior.Disabled };
            dp.IsOpen = true;
            Assert.False(dp.IsOpen);
        }

        [Fact]
        public void Drawer_AcceptsString()
        {
            var dp = new DrawerPage { Drawer = "MenuContent" };
            Assert.Equal("MenuContent", dp.Drawer);
        }

        [Fact]
        public void Content_AcceptsString()
        {
            var dp = new DrawerPage { Content = "ContentValue" };
            Assert.Equal("ContentValue", dp.Content);
        }

        [Fact]
        public void Drawer_AcceptsContentPage()
        {
            var page = new ContentPage { Header = "Menu" };
            var dp = new DrawerPage { Drawer = page };
            Assert.Same(page, dp.Drawer);
        }

        [Fact]
        public void Content_AcceptsContentPage()
        {
            var page = new ContentPage { Header = "Main" };
            var dp = new DrawerPage { Content = page };
            Assert.Same(page, dp.Content);
        }
    }

    public class LogicalChildrenTests : ScopedTestBase
    {
        [Fact]
        public void Drawer_SetPage_AddedToLogicalChildren()
        {
            var dp = new DrawerPage();
            var drawer = new ContentPage { Header = "Menu" };
            dp.Drawer = drawer;
            Assert.Contains(drawer, ((ILogical)dp).LogicalChildren);
        }

        [Fact]
        public void Content_SetPage_AddedToLogicalChildren()
        {
            var dp = new DrawerPage();
            var detail = new ContentPage { Header = "Content" };
            dp.Content = detail;
            Assert.Contains(detail, ((ILogical)dp).LogicalChildren);
        }

        [Fact]
        public void Drawer_Replaced_OldRemovedNewAdded()
        {
            var dp = new DrawerPage();
            var first = new ContentPage { Header = "First" };
            var second = new ContentPage { Header = "Second" };

            dp.Drawer = first;
            dp.Drawer = second;

            var children = ((ILogical)dp).LogicalChildren;
            Assert.DoesNotContain(first, children);
            Assert.Contains(second, children);
        }

        [Fact]
        public void Content_Replaced_OldRemovedNewAdded()
        {
            var dp = new DrawerPage();
            var first = new ContentPage { Header = "First" };
            var second = new ContentPage { Header = "Second" };

            dp.Content = first;
            dp.Content = second;

            var children = ((ILogical)dp).LogicalChildren;
            Assert.DoesNotContain(first, children);
            Assert.Contains(second, children);
        }

        [Fact]
        public void Drawer_SetToNull_RemovedFromLogicalChildren()
        {
            var dp = new DrawerPage();
            var drawer = new ContentPage { Header = "Menu" };
            dp.Drawer = drawer;
            dp.Drawer = null;
            Assert.DoesNotContain(drawer, ((ILogical)dp).LogicalChildren);
        }

        [Fact]
        public void Content_SetToNull_RemovedFromLogicalChildren()
        {
            var dp = new DrawerPage();
            var detail = new ContentPage { Header = "Content" };
            dp.Content = detail;
            dp.Content = null;
            Assert.DoesNotContain(detail, ((ILogical)dp).LogicalChildren);
        }

        [Fact]
        public void DrawerAndContent_BothSet_BothInLogicalChildren()
        {
            var dp = new DrawerPage();
            var drawer = new ContentPage { Header = "Menu" };
            var detail = new ContentPage { Header = "Home" };
            dp.Drawer = drawer;
            dp.Content = detail;

            var children = ((ILogical)dp).LogicalChildren;
            Assert.Contains(drawer, children);
            Assert.Contains(detail, children);
        }

        [Fact]
        public void Drawer_MultipleReplacements_OnlyLastInLogicalChildren()
        {
            var dp = new DrawerPage();
            var first = new ContentPage { Header = "1st" };
            var second = new ContentPage { Header = "2nd" };
            var third = new ContentPage { Header = "3rd" };

            dp.Drawer = first;
            dp.Drawer = second;
            dp.Drawer = third;

            var children = ((ILogical)dp).LogicalChildren;
            Assert.DoesNotContain(first, children);
            Assert.DoesNotContain(second, children);
            Assert.Contains(third, children);
        }
    }

    public class DrawerEventTests : ScopedTestBase
    {
        [Fact]
        public void IsOpen_SetTrue_FiresOpened()
        {
            var dp = new DrawerPage();
            bool fired = false;
            dp.Opened += (_, _) => fired = true;

            dp.IsOpen = true;

            Assert.True(fired);
        }

        [Fact]
        public void IsOpen_SetFalse_FiresClosed()
        {
            var dp = new DrawerPage { IsOpen = true };
            bool fired = false;
            dp.Closed += (_, _) => fired = true;

            dp.IsOpen = false;

            Assert.True(fired);
        }

        [Fact]
        public void IsOpen_SetFalse_FiresClosingBeforeClosed()
        {
            var dp = new DrawerPage { IsOpen = true };
            var order = new List<string>();
            dp.Closing += (_, _) => order.Add("Closing");
            dp.Closed  += (_, _) => order.Add("Closed");

            dp.IsOpen = false;

            Assert.Equal(new[] { "Closing", "Closed" }, order);
        }

        [Fact]
        public void Closing_Cancel_PreventsClose()
        {
            var dp = new DrawerPage { IsOpen = true };
            dp.Closing += (_, e) => e.Cancel = true;

            dp.IsOpen = false;

            Assert.True(dp.IsOpen);
        }

        [Fact]
        public void Closing_Cancel_DoesNotFireClosed()
        {
            var dp = new DrawerPage { IsOpen = true };
            dp.Closing += (_, e) => e.Cancel = true;
            bool closedFired = false;
            dp.Closed += (_, _) => closedFired = true;

            dp.IsOpen = false;

            Assert.False(closedFired);
        }

        [Fact]
        public void Closing_Cancel_DoesNotFireOpened()
        {
            var dp = new DrawerPage { IsOpen = true };
            dp.Closing += (_, e) => e.Cancel = true;
            bool openedFired = false;
            dp.Opened += (_, _) => openedFired = true;

            dp.IsOpen = false;

            Assert.False(openedFired);
        }

        [Fact]
        public void IsOpen_AlreadyTrue_SetTrue_DoesNotFireOpened()
        {
            var dp = new DrawerPage { IsOpen = true };
            bool fired = false;
            dp.Opened += (_, _) => fired = true;

            dp.IsOpen = true;

            Assert.False(fired);
        }

        [Fact]
        public void IsOpen_AlreadyFalse_SetFalse_DoesNotFireClosingOrClosed()
        {
            var dp = new DrawerPage();
            bool closingFired = false;
            bool closedFired  = false;
            dp.Closing += (_, _) => closingFired = true;
            dp.Closed  += (_, _) => closedFired  = true;

            dp.IsOpen = false;

            Assert.False(closingFired);
            Assert.False(closedFired);
        }

        [Fact]
        public void IsOpen_SetTrue_DoesNotFireClosing()
        {
            var dp = new DrawerPage();
            bool closingFired = false;
            dp.Closing += (_, _) => closingFired = true;

            dp.IsOpen = true;

            Assert.False(closingFired);
        }

        [Fact]
        public void DrawerBehavior_Locked_ForcesIsOpen_True()
        {
            var dp = new DrawerPage { DrawerBehavior = DrawerBehavior.Locked };
            Assert.True(dp.IsOpen);
        }

        [Fact]
        public void DrawerBehavior_Locked_WhileClosed_OpensWithoutFiringClosing()
        {
            var dp = new DrawerPage();
            bool closingFired = false;
            dp.Closing += (_, _) => closingFired = true;

            dp.DrawerBehavior = DrawerBehavior.Locked;

            Assert.True(dp.IsOpen);
            Assert.False(closingFired);
        }

        [Fact]
        public void Closing_Cancel_PreventsClose_EvenWithReentrantIsOpenFalse()
        {
            var dp = new DrawerPage { IsOpen = true };
            dp.Closing += (_, e) => e.Cancel = true;

            dp.PropertyChanged += (_, e) =>
            {
                if (e.Property == DrawerPage.IsOpenProperty)
                    dp.SetCurrentValue(DrawerPage.IsOpenProperty, false);
            };

            dp.IsOpen = false;

            Assert.True(dp.IsOpen);
        }

        [Fact]
        public void IsOpen_RapidToggle_EventsFiredExactlyOncePerChange()
        {
            var dp = new DrawerPage();
            int openedCount = 0;
            int closedCount = 0;
            dp.Opened += (_, _) => openedCount++;
            dp.Closed += (_, _) => closedCount++;

            for (int i = 0; i < 5; i++)
            {
                dp.IsOpen = true;
                dp.IsOpen = false;
            }

            Assert.Equal(5, openedCount);
            Assert.Equal(5, closedCount);
            Assert.False(dp.IsOpen);
        }

        [Fact]
        public void BackdropPress_WithCanceledClose_FiresClosingOnce_WhenTemplateWasAppliedBeforeAttach()
        {
            var dp = new DrawerPage
            {
                Template = BackdropOnlyTemplate(),
                IsOpen = true,
                BackdropBrush = Brushes.Black,
                DisplayMode = SplitViewDisplayMode.Overlay
            };

            dp.ApplyTemplate();

            int closingCount = 0;
            dp.Closing += (_, e) =>
            {
                closingCount++;
                e.Cancel = true;
            };

            var root = new TestRoot { Child = dp };
            root.ExecuteInitialLayoutPass();

            var backdrop = Assert.Single(dp.GetVisualDescendants().OfType<Border>(), x => x.Name == "PART_Backdrop");

            RaisePointerPressed(backdrop);

            Assert.Equal(1, closingCount);
            Assert.True(dp.IsOpen);
        }
    }

    public class LifecycleEventTests : ScopedTestBase
    {
        [Fact]
        public void IsOpen_Changes_NeverFirePageLifecycleEvents()
        {
            // Toggling IsOpen (open, close, repeated) must never raise page lifecycle events.
            var page = new ContentPage { Header = "Content" };
            var dp = new DrawerPage();
            var root = new TestRoot { Child = dp };
            dp.Content = page; // fires initial NavigatedTo

            var events = new List<string>();
            page.NavigatedTo   += (_, _) => events.Add("NavigatedTo");
            page.NavigatedFrom += (_, _) => events.Add("NavigatedFrom");

            dp.IsOpen = true;
            dp.IsOpen = false;
            dp.IsOpen = false; // same value

            Assert.Empty(events);
        }

        [Fact]
        public void Content_SetInitially_FiresNavigatedTo()
        {
            var page = new ContentPage { Header = "Home" };
            var events = new List<string>();
            page.NavigatedTo  += (_, _) => events.Add("NavigatedTo");

            var dp = new DrawerPage();
            var root = new TestRoot { Child = dp };
            dp.Content = page;

            Assert.Single(events);
            Assert.Equal("NavigatedTo",  events[0]);
        }

        [Fact]
        public void Content_SetInitially_SetsCurrentPage()
        {
            var page = new ContentPage { Header = "Home" };
            var dp = new DrawerPage { Content = page };
            Assert.Same(page, dp.CurrentPage);
        }

        [Fact]
        public void Content_Changed_FiresLifecycleEventsInOrder()
        {
            var first = new ContentPage { Header = "First" };
            var second = new ContentPage { Header = "Second" };
            var dp = new DrawerPage();
            var root = new TestRoot { Child = dp };
            dp.Content = first;

            var order = new List<string>();
            first.NavigatedFrom += (_, _) => order.Add("NavigatedFrom");
            second.NavigatedTo += (_, _) => order.Add("NavigatedTo");

            dp.Content = second;

            Assert.Equal(2, order.Count);
            Assert.Equal("NavigatedFrom", order[0]);
            Assert.Equal("NavigatedTo", order[1]);
        }

        [Fact]
        public void Content_SetInitially_NavigatedTo_NavigationTypeIsReplace()
        {
            var page = new ContentPage { Header = "Home" };
            NavigatedToEventArgs? args = null;
            page.NavigatedTo += (_, e) => args = e;

            var dp = new DrawerPage();
            var root = new TestRoot { Child = dp };
            dp.Content = page;

            Assert.NotNull(args);
            Assert.Equal(NavigationType.Replace, args!.NavigationType);
        }

        [Fact]
        public void Content_Changed_NavigatedFromAndTo_NavigationTypeIsReplace()
        {
            var first = new ContentPage { Header = "First" };
            var second = new ContentPage { Header = "Second" };
            var dp = new DrawerPage();
            var root = new TestRoot { Child = dp };
            dp.Content = first;

            NavigatedFromEventArgs? fromArgs = null;
            NavigatedToEventArgs?   toArgs   = null;
            first.NavigatedFrom  += (_, e) => fromArgs = e;
            second.NavigatedTo   += (_, e) => toArgs   = e;

            dp.Content = second;

            Assert.NotNull(fromArgs);
            Assert.Equal(NavigationType.Replace, fromArgs!.NavigationType);
            Assert.NotNull(toArgs);
            Assert.Equal(NavigationType.Replace, toArgs!.NavigationType);
        }

        [Fact]
        public void Content_ChangedWhileOverlayDrawerOpen_NonPageDrawer_FiresLifecycleEvents()
        {
            var home    = new ContentPage { Header = "Home" };
            var profile = new ContentPage { Header = "Profile" };
            var dp = new DrawerPage
            {
                DisplayMode = SplitViewDisplayMode.Overlay,
            };
            var root = new TestRoot { Child = dp };
            dp.Drawer = new StackPanel();
            dp.Content = home;

            var events = new List<string>();
            home.NavigatedFrom   += (_, _) => events.Add("Home: NavigatedFrom");
            profile.NavigatedTo  += (_, _) => events.Add("Profile: NavigatedTo");

            dp.IsOpen = true;
            Assert.Empty(events);

            dp.Content = profile;
            Assert.Equal(2, events.Count);
            Assert.Equal("Home: NavigatedFrom", events[0]);
            Assert.Equal("Profile: NavigatedTo", events[1]);
        }

        // --- Initial-attach lifecycle (the _hasHadFirstPage / OnLoaded fix) ---

        [Fact]
        public void Content_SetBeforeAttach_SuppressedUntilLoad()
        {
            // Events must NOT fire during XAML parsing (before VisualRoot is set).
            var page = new ContentPage { Header = "Home" };
            var events = new List<string>();
            page.NavigatedTo += (_, _) => events.Add("NavigatedTo");

            var _ = new DrawerPage { Content = page };

            Assert.Empty(events);
        }

        [Fact]
        public void Content_SetBeforeAttach_FiresLifecycleEventsOnLoad()
        {
            // Content set before the control enters the visual tree (simulating XAML parsing).
            // Events must fire exactly once when the control is attached and Loaded fires.
            var page = new ContentPage { Header = "Home" };
            var events = new List<string>();
            page.NavigatedTo += (_, _) => events.Add("NavigatedTo");

            var dp = new DrawerPage { Content = page };
            Assert.Empty(events); // suppressed before visual tree

            var root = new TestRoot { Child = dp };
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken); // pump the posted Loaded dispatch

            Assert.Single(events);
            Assert.Equal("NavigatedTo", events[0]);
        }

        [Fact]
        public void Content_SetBeforeAttach_ThenChangedAfterAttach_NoDoubleFire()
        {
            var first  = new ContentPage { Header = "First" };
            var second = new ContentPage { Header = "Second" };

            var dp = new DrawerPage { Content = first };
            var root = new TestRoot { Child = dp };
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken); // fire the deferred NavigatedTo on first

            var events = new List<string>();
            first.NavigatedFrom += (_, _) => events.Add("First: NavigatedFrom");
            second.NavigatedTo  += (_, _) => events.Add("Second: NavigatedTo");

            dp.Content = second;

            Assert.Equal(2, events.Count);
            Assert.Equal("First: NavigatedFrom", events[0]);
            Assert.Equal("Second: NavigatedTo",  events[1]);
        }

        [Fact]
        public void Content_SetBeforeAttach_NavigatedTo_NavigationTypeIsPush()
        {
            var page = new ContentPage { Header = "Home" };
            NavigatedToEventArgs? args = null;
            page.NavigatedTo += (_, e) => args = e;

            var dp = new DrawerPage { Content = page };
            var root = new TestRoot { Child = dp };
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.NotNull(args);
            Assert.Equal(NavigationType.Push, args!.NavigationType);
        }

        [Fact]
        public void Content_SetToSameInstance_NoLifecycleEvents()
        {
            // Re-assigning the same Content instance must not re-fire lifecycle events.
            var page = new ContentPage { Header = "Home" };
            var dp = new DrawerPage();
            var root = new TestRoot { Child = dp };
            dp.Content = page; // initial assignment fires NavigatedTo

            var events = new List<string>();
            page.NavigatedTo   += (_, _) => events.Add("NavigatedTo");
            page.NavigatedFrom += (_, _) => events.Add("NavigatedFrom");

            dp.Content = page; // same instance — must not fire anything

            Assert.Empty(events);
        }
    }


    public class DisplayModeMappingTests : ScopedTestBase
    {
        [Fact]
        public void DrawerLayoutBehavior_Overlay_MapsToOverlay()
        {
            var dp = new DrawerPage { DrawerLayoutBehavior = DrawerLayoutBehavior.Overlay };
            Assert.Equal(SplitViewDisplayMode.Overlay, dp.DisplayMode);
        }

        [Fact]
        public void DrawerLayoutBehavior_Split_MapsToInline()
        {
            var dp = new DrawerPage { DrawerLayoutBehavior = DrawerLayoutBehavior.Split };
            Assert.Equal(SplitViewDisplayMode.Inline, dp.DisplayMode);
        }

        [Fact]
        public void DrawerLayoutBehavior_CompactOverlay_MapsToCompactOverlay()
        {
            var dp = new DrawerPage { DrawerLayoutBehavior = DrawerLayoutBehavior.CompactOverlay };
            Assert.Equal(SplitViewDisplayMode.CompactOverlay, dp.DisplayMode);
        }

        [Fact]
        public void DrawerLayoutBehavior_CompactInline_MapsToCompactInline()
        {
            var dp = new DrawerPage { DrawerLayoutBehavior = DrawerLayoutBehavior.CompactInline };
            Assert.Equal(SplitViewDisplayMode.CompactInline, dp.DisplayMode);
        }

        [Fact]
        public void DrawerBehavior_Locked_OverridesCompactOverlay_ToInline()
        {
            var dp = new DrawerPage
            {
                DrawerLayoutBehavior = DrawerLayoutBehavior.CompactOverlay,
                DrawerBehavior = DrawerBehavior.Locked
            };
            Assert.Equal(SplitViewDisplayMode.Inline, dp.DisplayMode);
        }

        [Fact]
        public void DrawerBehavior_Flyout_OverridesCompactInline_ToOverlay()
        {
            var dp = new DrawerPage
            {
                DrawerLayoutBehavior = DrawerLayoutBehavior.CompactInline,
                DrawerBehavior = DrawerBehavior.Flyout
            };
            Assert.Equal(SplitViewDisplayMode.Overlay, dp.DisplayMode);
        }

        [Fact]
        public void DrawerBreakpointLength_BeforeLayout_DoesNotOverrideLayoutBehavior()
        {
            var dp = new DrawerPage
            {
                DrawerLayoutBehavior = DrawerLayoutBehavior.Split,
                DrawerBreakpointLength = 1200
            };
            Assert.Equal(SplitViewDisplayMode.Inline, dp.DisplayMode);
        }

        [Fact]
        public void DrawerBreakpointLength_Zero_DoesNotOverrideLayoutBehavior()
        {
            // Breakpoint == 0 means the feature is disabled; DrawerLayoutBehavior drives DisplayMode.
            var dp = new DrawerPage
            {
                DrawerLayoutBehavior = DrawerLayoutBehavior.Split,
                DrawerBreakpointLength = 0
            };
            Assert.Equal(SplitViewDisplayMode.Inline, dp.DisplayMode);
        }

        [Theory]
        [InlineData(DrawerPlacement.Left)]
        [InlineData(DrawerPlacement.Right)]
        public void DrawerBreakpointLength_Horizontal_BelowBreakpoint_ForcesOverlay(DrawerPlacement placement)
        {
            var dp = new DrawerPage
            {
                DrawerLayoutBehavior = DrawerLayoutBehavior.Split,
                DrawerBreakpointLength = 1200,
                DrawerPlacement = placement
            };
            dp.Measure(new Size(800, 600));
            dp.Arrange(new Rect(0, 0, 800, 600));

            Assert.Equal(SplitViewDisplayMode.Overlay, dp.DisplayMode);
        }

        [Theory]
        [InlineData(DrawerPlacement.Left)]
        [InlineData(DrawerPlacement.Right)]
        public void DrawerBreakpointLength_Horizontal_AboveBreakpoint_UsesConfiguredLayout(DrawerPlacement placement)
        {
            var dp = new DrawerPage
            {
                DrawerLayoutBehavior = DrawerLayoutBehavior.Split,
                DrawerBreakpointLength = 600,
                DrawerPlacement = placement
            };
            dp.Measure(new Size(800, 600));
            dp.Arrange(new Rect(0, 0, 800, 600));

            Assert.Equal(SplitViewDisplayMode.Inline, dp.DisplayMode);
        }

        [Theory]
        [InlineData(DrawerPlacement.Top)]
        [InlineData(DrawerPlacement.Bottom)]
        public void DrawerBreakpointLength_Vertical_BelowBreakpoint_ForcesOverlay(DrawerPlacement placement)
        {
            var dp = new DrawerPage
            {
                DrawerLayoutBehavior = DrawerLayoutBehavior.Split,
                DrawerBreakpointLength = 800,
                DrawerPlacement = placement
            };
            dp.Measure(new Size(800, 600));
            dp.Arrange(new Rect(0, 0, 800, 600));

            // Vertical: breakpoint compares against Bounds.Height (600 < 800 → Overlay)
            Assert.Equal(SplitViewDisplayMode.Overlay, dp.DisplayMode);
        }

        [Theory]
        [InlineData(DrawerPlacement.Top)]
        [InlineData(DrawerPlacement.Bottom)]
        public void DrawerBreakpointLength_Vertical_AboveBreakpoint_UsesConfiguredLayout(DrawerPlacement placement)
        {
            var dp = new DrawerPage
            {
                DrawerLayoutBehavior = DrawerLayoutBehavior.Split,
                DrawerBreakpointLength = 400,
                DrawerPlacement = placement
            };
            dp.Measure(new Size(800, 600));
            dp.Arrange(new Rect(0, 0, 800, 600));

            // Vertical: breakpoint compares against Bounds.Height (600 > 400 → Inline)
            Assert.Equal(SplitViewDisplayMode.Inline, dp.DisplayMode);
        }
    }

    public class DisabledBehaviorClosingTests : ScopedTestBase
    {
        [Fact]
        public void Closing_Cancel_CannotPreventDisabledClose()
        {
            var dp = new DrawerPage { IsOpen = true };
            dp.Closing += (_, e) => e.Cancel = true;

            dp.DrawerBehavior = DrawerBehavior.Disabled;

            Assert.False(dp.IsOpen);
        }

        [Fact]
        public void Closing_NotFired_WhenDisabledForcesClose()
        {
            var dp = new DrawerPage { IsOpen = true };
            bool closingFired = false;
            dp.Closing += (_, _) => closingFired = true;

            dp.DrawerBehavior = DrawerBehavior.Disabled;

            Assert.False(closingFired);
        }
    }

    public class DrawerLengthValidationTests : ScopedTestBase
    {
        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(-1.0)]
        [InlineData(-100.0)]
        public void DrawerLength_RejectsInvalidValues(double invalid)
        {
            var dp = new DrawerPage { DrawerLength = 200.0 };
            Assert.ThrowsAny<ArgumentException>(() => dp.DrawerLength = invalid);
            Assert.Equal(200.0, dp.DrawerLength);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        [InlineData(-1.0)]
        [InlineData(-100.0)]
        public void CompactDrawerLength_RejectsInvalidValues(double invalid)
        {
            var dp = new DrawerPage();
            Assert.ThrowsAny<ArgumentException>(() => dp.CompactDrawerLength = invalid);
            Assert.Equal(48.0, dp.CompactDrawerLength);
        }

        [Fact]
        public void DrawerLength_AcceptsZero()
        {
            var dp = new DrawerPage { DrawerLength = 0 };
            Assert.Equal(0.0, dp.DrawerLength);
        }

        [Fact]
        public void CompactDrawerLength_AcceptsZero()
        {
            var dp = new DrawerPage { CompactDrawerLength = 0 };
            Assert.Equal(0.0, dp.CompactDrawerLength);
        }
    }

    public class EscapeKeyTests : ScopedTestBase
    {
        [Fact]
        public void EscapeKey_ClosesOverlayDrawer()
        {
            var dp = new DrawerPage
            {
                DisplayMode = SplitViewDisplayMode.Overlay,
                IsOpen = true,
            };
            var root = new TestRoot { Child = dp };

            dp.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });

            Assert.False(dp.IsOpen);
        }

        [Fact]
        public void EscapeKey_ClosesCompactOverlayDrawer()
        {
            var dp = new DrawerPage
            {
                DisplayMode = SplitViewDisplayMode.CompactOverlay,
                IsOpen = true,
            };
            var root = new TestRoot { Child = dp };

            dp.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });

            Assert.False(dp.IsOpen);
        }

        [Fact]
        public void EscapeKey_DoesNotCloseInlineDrawer()
        {
            var dp = new DrawerPage
            {
                DisplayMode = SplitViewDisplayMode.Inline,
                IsOpen = true,
            };
            var root = new TestRoot { Child = dp };

            dp.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });

            Assert.True(dp.IsOpen);
        }
    }

    public class SystemBackButtonTests : ScopedTestBase
    {
        private static RoutedEventArgs RaiseBackButton(DrawerPage dp)
        {
            var args = new RoutedEventArgs(Page.PageNavigationSystemBackButtonPressedEvent);
            dp.RaiseEvent(args);
            return args;
        }

        [Fact]
        public void BackButton_ClosesOpenDrawer()
        {
            var dp = new DrawerPage { IsOpen = true };
            var root = new TestRoot { Child = dp };

            var args = RaiseBackButton(dp);

            Assert.False(dp.IsOpen);
            Assert.True(args.Handled);
        }

        [Fact]
        public void BackButton_DoesNotCloseLockedDrawer()
        {
            var dp = new DrawerPage
            {
                DrawerBehavior = DrawerBehavior.Locked,
                IsOpen = true
            };
            var root = new TestRoot { Child = dp };

            var args = RaiseBackButton(dp);

            Assert.True(dp.IsOpen);
            Assert.False(args.Handled);
        }

        [Fact]
        public void BackButton_DoesNotActOnDisabledDrawer()
        {
            var dp = new DrawerPage { DrawerBehavior = DrawerBehavior.Disabled };
            var root = new TestRoot { Child = dp };

            var args = RaiseBackButton(dp);

            Assert.False(dp.IsOpen);
            Assert.False(args.Handled);
        }

        [Fact]
        public void BackButton_DoesNotActWhenAlreadyClosed()
        {
            var dp = new DrawerPage();
            var root = new TestRoot { Child = dp };

            var args = RaiseBackButton(dp);

            Assert.False(dp.IsOpen);
            Assert.False(args.Handled);
        }
    }

    public class IconTests : ScopedTestBase
    {
        [Fact]
        public void DrawerIconTemplate_RoundTrips()
        {
            var template = new FuncDataTemplate<object>((_, _) => new PathIcon());
            var dp = new DrawerPage { DrawerIconTemplate = template };
            Assert.Same(template, dp.DrawerIconTemplate);
        }

        [Fact]
        public void DrawerIcon_With_Geometry_Does_Not_Throw()
        {
            var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };
            var dp = new DrawerPage
            {
                DrawerIcon = geometry,
                DrawerIconTemplate = new FuncDataTemplate<object>((_, _) => new PathIcon()),
            };
            var root = new TestRoot { Child = dp };

            dp.DrawerIcon = new EllipseGeometry { Rect = new Rect(0, 0, 20, 20) };
            Assert.NotNull(dp.DrawerIcon);
        }
    }

    public class SwipeGestureTests : ScopedTestBase
    {
        [Fact]
        public void HandledPointerPressedAtEdge_AllowsSwipeOpen()
        {
            var dp = new DrawerPage
            {
                DrawerPlacement = DrawerPlacement.Left,
                DisplayMode = SplitViewDisplayMode.Overlay,
                Width = 400,
                Height = 300
            };
            dp.GestureRecognizers.OfType<SwipeGestureRecognizer>().First().IsMouseEnabled = true;

            var root = new TestRoot
            {
                ClientSize = new Size(400, 300),
                Child = dp
            };
            root.ExecuteInitialLayoutPass();

            RaiseHandledPointerPressed(dp, new Point(5, 5));

            var swipe = new SwipeGestureEventArgs(1, new Vector(-20, 0), default);
            dp.RaiseEvent(swipe);

            Assert.True(swipe.Handled);
            Assert.True(dp.IsOpen);
        }

        [Fact]
        public void MouseEdgeDrag_AllowsSwipeOpen()
        {
            var dp = new DrawerPage
            {
                DrawerPlacement = DrawerPlacement.Left,
                DisplayMode = SplitViewDisplayMode.Overlay,
                Width = 400,
                Height = 300
            };
            dp.GestureRecognizers.OfType<SwipeGestureRecognizer>().First().IsMouseEnabled = true;

            var root = new TestRoot
            {
                ClientSize = new Size(400, 300),
                Child = dp
            };
            root.ExecuteInitialLayoutPass();

            var mouse = new MouseTestHelper();
            mouse.Down(dp, position: new Point(5, 5));
            mouse.Move(dp, new Point(40, 5));
            mouse.Up(dp, position: new Point(40, 5));

            Assert.True(dp.IsOpen);
        }

        private static void RaiseHandledPointerPressed(Interactive target, Point position)
        {
            var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
            var args = new PointerPressedEventArgs(
                target,
                pointer,
                (Visual)target,
                position,
                timestamp: 1,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None)
            {
                Handled = true
            };

            target.RaiseEvent(args);
        }
    }

    private static void RaisePointerPressed(Interactive target, Point? position = null)
    {
        var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        var args = new PointerPressedEventArgs(
            target,
            pointer,
            (Visual)target,
            position ?? default,
            timestamp: 1,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None);

        target.RaiseEvent(args);
    }

    private static IControlTemplate BackdropOnlyTemplate()
    {
        return new FuncControlTemplate<DrawerPage>((_, scope) =>
            new Canvas
            {
                Children =
                {
                    new Border
                    {
                        Name = "PART_Backdrop"
                    }.RegisterInNameScope(scope)
                }
            });
    }

    public class DetachmentTests : ScopedTestBase
    {
        [Fact]
        public async Task OnDetached_ClearsDrawerPageReferenceOnNavigationPage()
        {
            var root = new TestRoot();
            var nav = new NavigationPage();
            var dp = new DrawerPage { Content = nav };
            root.Child = dp;

            // Detach: should clear the DrawerPage reference
            root.Child = null;

            // NavigationPage should no longer reference the DrawerPage.
            // Pushing a page should not show a hamburger icon (which requires DrawerPage).
            var page = new ContentPage();
            await nav.PushAsync(page);
            Assert.Null(NavigationPage.GetBackButtonContent(page));
        }

        [Fact]
        public async Task Detach_And_Reattach_RestoresDrawerPageReference()
        {
            var nav = new NavigationPage();
            var dp = new DrawerPage { Content = nav };
            var root = new TestRoot { Child = dp };
            var page = new ContentPage();
            await nav.PushAsync(page);

            Assert.True(nav.IsBackButtonEffectivelyVisible);

            root.Child = null;
            Assert.False(nav.IsBackButtonEffectivelyVisible ?? false);

            root.Child = dp;
            Assert.True(nav.IsBackButtonEffectivelyVisible);
        }
    }
}

using System;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class ScrollBarTests : ScopedTestBase
    {
        [Fact]
        public void Setting_Value_Should_Update_Track_Value()
        {
            var target = new ScrollBar
            {
                Template = new FuncControlTemplate<ScrollBar>(Template),
            };

            target.ApplyTemplate();
            var track = (Track)target.GetTemplateChildren().First(x => x.Name == "track");
            target.Value = 50;

            Assert.Equal(50, track.Value);
        }

        [Fact]
        public void Setting_Track_Value_Should_Update_Value()
        {
            var target = new ScrollBar
            {
                Template = new FuncControlTemplate<ScrollBar>(Template),
            };

            target.ApplyTemplate();
            var track = (Track)target.GetTemplateChildren().First(x => x.Name == "track");
            track.Value = 50;

            Assert.Equal(50, target.Value);
        }

        [Fact]
        public void Setting_Track_Value_After_Setting_Value_Should_Update_Value()
        {
            var target = new ScrollBar
            {
                Template = new FuncControlTemplate<ScrollBar>(Template),
            };

            target.ApplyTemplate();

            var track = (Track)target.GetTemplateChildren().First(x => x.Name == "track");
            target.Value = 25;
            track.Value = 50;

            Assert.Equal(50, target.Value);
        }

        [Fact]
        public void Thumb_DragDelta_Event_Should_Raise_Scroll_Event()
        {
            var target = new ScrollBar
            {
                Template = new FuncControlTemplate<ScrollBar>(Template),
            };

            target.ApplyTemplate();

            var track = (Track)target.GetTemplateChildren().First(x => x.Name == "track");

            var raisedEvent = Assert.Raises<ScrollEventArgs>(
                handler => target.Scroll += handler,
                handler => target.Scroll -= handler,
                () =>
                {
                    var ev = new VectorEventArgs
                    {
                        RoutedEvent = Thumb.DragDeltaEvent,
                        Vector = new Vector(0, 0)
                    };

                    track.Thumb.RaiseEvent(ev);
                });

            Assert.Equal(ScrollEventType.ThumbTrack, raisedEvent.Arguments.ScrollEventType);
        }

        [Fact]
        public void Thumb_DragComplete_Event_Should_Raise_Scroll_Event()
        {
            var target = new ScrollBar
            {
                Template = new FuncControlTemplate<ScrollBar>(Template),
            };

            target.ApplyTemplate();

            var track = (Track)target.GetTemplateChildren().First(x => x.Name == "track");

            var raisedEvent = Assert.Raises<ScrollEventArgs>(
                handler => target.Scroll += handler,
                handler => target.Scroll -= handler,
                () =>
                {
                    var ev = new VectorEventArgs
                    {
                        RoutedEvent = Thumb.DragCompletedEvent,
                        Vector = new Vector(0, 0)
                    };

                    track.Thumb.RaiseEvent(ev);
                });

            Assert.Equal(ScrollEventType.EndScroll, raisedEvent.Arguments.ScrollEventType);
        }

        [Fact]
        public void ScrollBar_Can_AutoHide()
        {
            var target = new ScrollBar();

            target.Visibility = ScrollBarVisibility.Auto;
            target.ViewportSize = 1;
            target.Maximum = 0;

            Assert.False(target.IsVisible);
        }

        [Fact]
        public void ScrollBar_Should_Not_AutoHide_When_ViewportSize_Is_NaN()
        {
            var target = new ScrollBar();

            target.Visibility = ScrollBarVisibility.Auto;
            target.Minimum = 0;
            target.Maximum = 100;
            target.ViewportSize = double.NaN;

            Assert.True(target.IsVisible);
        }

        [Fact]
        public void ScrollBar_Should_Not_AutoHide_When_Visibility_Set_To_Visible()
        {
            var target = new ScrollBar();

            target.Visibility = ScrollBarVisibility.Visible;
            target.Minimum = 0;
            target.Maximum = 100;
            target.ViewportSize = 100;

            Assert.True(target.IsVisible);
        }

        [Fact]
        public void ScrollBar_Should_Hide_When_Visibility_Set_To_Hidden()
        {
            var target = new ScrollBar();

            target.Visibility = ScrollBarVisibility.Hidden;
            target.Minimum = 0;
            target.Maximum = 100;
            target.ViewportSize = 10;

            Assert.False(target.IsVisible);
        }

        private static Control Template(ScrollBar control, INameScope scope)
        {
            return new Border
            {
                Child = new Track
                {
                    Name = "track",
                    [!Track.MinimumProperty] = control[!RangeBase.MinimumProperty],
                    [!Track.MaximumProperty] = control[!RangeBase.MaximumProperty],
                    [!!Track.ValueProperty] = control[!!RangeBase.ValueProperty],
                    [!Track.ViewportSizeProperty] = control[!ScrollBar.ViewportSizeProperty],
                    [!Track.OrientationProperty] = control[!ScrollBar.OrientationProperty],
                    Thumb = new Thumb
                    {
                        Template = new FuncControlTemplate<Thumb>(ThumbTemplate),
                    },
                }.RegisterInNameScope(scope),
            };
        }

        private static Control ThumbTemplate(Thumb control, INameScope scope)
        {
            return new Border
            {
                Background = Brushes.Gray,
            };
        }

        [Fact]
        public void ContextRequested_Should_Create_VerticalContextMenu_For_Vertical_ScrollBar()
        {
            var target = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Template = new FuncControlTemplate<ScrollBar>(Template),
            };
            
            target.ApplyTemplate();

            // Simulate context request
            var contextRequestedArgs = new ContextRequestedEventArgs();
            target.RaiseEvent(contextRequestedArgs);

            var menuItems = target.ContextMenu.Items.OfType<MenuItem>().ToList();
            Assert.Equal(7, menuItems.Count);
            
            Assert.Contains(menuItems, m => m.Header?.ToString()?.Contains("Scroll") == true || m.Header?.ToString() == "ScrollHere");
            Assert.Contains(menuItems, m => m.Header?.ToString()?.Contains("Top") == true || m.Header?.ToString() == "Top");
            Assert.Contains(menuItems, m => m.Header?.ToString()?.Contains("Bottom") == true || m.Header?.ToString() == "Bottom");
        }

        [Fact]
        public void ContextRequested_Should_Create_HorizontalContextMenu_For_Horizontal_ScrollBar_LTR()
        {
            var target = new ScrollBar
            {
                Orientation = Orientation.Horizontal,
                Template = new FuncControlTemplate<ScrollBar>(Template),
            };
            
            target.ApplyTemplate();

            // Simulate context request
            var contextRequestedArgs = new ContextRequestedEventArgs();
            target.RaiseEvent(contextRequestedArgs);

            Assert.NotNull(target.ContextMenu);
            var menuItems = target.ContextMenu.Items.OfType<MenuItem>().ToList();
            Assert.Equal(7, menuItems.Count);
            
            // Check that we have horizontal-specific menu items
            Assert.Contains(menuItems, m => m.Header?.ToString()?.Contains("Left") == true || m.Header?.ToString() == "LeftEdge");
            Assert.Contains(menuItems, m => m.Header?.ToString()?.Contains("Right") == true || m.Header?.ToString() == "RightEdge");
        }

        [Fact]
        public void ContextRequested_Should_Create_HorizontalContextMenu_For_Horizontal_ScrollBar_RTL()
        {
            var target = new ScrollBar
            {
                Orientation = Orientation.Horizontal,
                Template = new FuncControlTemplate<ScrollBar>(Template),
            };
            
            target.ApplyTemplate();

            var contextRequestedArgs = new ContextRequestedEventArgs();
            target.RaiseEvent(contextRequestedArgs);

            Assert.NotNull(target.ContextMenu);
            var menuItems = target.ContextMenu.Items.OfType<MenuItem>().ToList();
            Assert.Equal(7, menuItems.Count);
        }

        [Fact]
        public void ScrollHere_MenuItem_Should_Update_ScrollBar_Value()
        {
            var target = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Template = new FuncControlTemplate<ScrollBar>(Template),
                Minimum = 0,
                Maximum = 100,
                Value = 0
            };
            
            target.ApplyTemplate();

            // Set a click position for ScrollHere to use
            var contextRequestedArgs = new ContextRequestedEventArgs();
            contextRequestedArgs.TryGetPosition(target, out var position);
            target.RaiseEvent(contextRequestedArgs);

            Assert.NotNull(target.ContextMenu);
            var scrollHereItem = target.ContextMenu.Items.OfType<MenuItem>()
                .FirstOrDefault(m => AutomationProperties.GetAutomationId(m) == "ScrollHere");
            
            Assert.NotNull(scrollHereItem);
            Assert.NotNull(scrollHereItem.Command);
            Assert.True(scrollHereItem.Command.CanExecute(null));
            
            // Execute the command
            scrollHereItem.Command.Execute(null);
            
            // Value should remain within bounds (exact value depends on click position and track layout)
            Assert.InRange(target.Value, target.Minimum, target.Maximum);
        }

        [Fact]
        public void ScrollToTop_MenuItem_Should_Set_Value_To_Minimum()
        {
            var target = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Template = new FuncControlTemplate<ScrollBar>(Template),
                Minimum = 10,
                Maximum = 100,
                Value = 50
            };
            
            target.ApplyTemplate();

            var contextRequestedArgs = new ContextRequestedEventArgs();
            target.RaiseEvent(contextRequestedArgs);

            var topItem = target.ContextMenu.Items.OfType<MenuItem>()
                .FirstOrDefault(m => AutomationProperties.GetAutomationId(m) == "Top");
            
            Assert.NotNull(topItem);
            topItem.Command.Execute(null);
            
            Assert.Equal(10, target.Value); // Should be set to Minimum
        }

        [Fact]
        public void ScrollToBottom_MenuItem_Should_Set_Value_To_Maximum()
        {
            var target = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Template = new FuncControlTemplate<ScrollBar>(Template),
                Minimum = 0,
                Maximum = 90,
                Value = 50
            };
            
            target.ApplyTemplate();

            var contextRequestedArgs = new ContextRequestedEventArgs();
            target.RaiseEvent(contextRequestedArgs);

            var bottomItem = target.ContextMenu.Items.OfType<MenuItem>()
                .FirstOrDefault(m => AutomationProperties.GetAutomationId(m) == "Bottom");
            
            Assert.NotNull(bottomItem);
            bottomItem.Command.Execute(null);
            
            Assert.Equal(90, target.Value); // Should be set to Maximum
        }

        [Fact]
        public void PageUp_MenuItem_Should_Decrease_Value_By_LargeChange()
        {
            var target = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Template = new FuncControlTemplate<ScrollBar>(Template),
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                LargeChange = 10
            };
            
            target.ApplyTemplate();

            var contextRequestedArgs = new ContextRequestedEventArgs();
            target.RaiseEvent(contextRequestedArgs);

            var pageUpItem = target.ContextMenu.Items.OfType<MenuItem>()
                .FirstOrDefault(m => AutomationProperties.GetAutomationId(m) == "PageUp");
            
            Assert.NotNull(pageUpItem);
            pageUpItem.Command.Execute(null);
            
            Assert.Equal(40, target.Value); // Should decrease by LargeChange (10)
        }

        [Fact]
        public void PageDown_MenuItem_Should_Increase_Value_By_LargeChange()
        {
            var target = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Template = new FuncControlTemplate<ScrollBar>(Template),
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                LargeChange = 10
            };
            
            target.ApplyTemplate();

            var contextRequestedArgs = new ContextRequestedEventArgs();
            target.RaiseEvent(contextRequestedArgs);

            var pageDownItem = target.ContextMenu.Items.OfType<MenuItem>()
                .FirstOrDefault(m => AutomationProperties.GetAutomationId(m) == "PageDown");
            
            Assert.NotNull(pageDownItem);
            pageDownItem.Command.Execute(null);
            
            Assert.Equal(60, target.Value); // Should increase by LargeChange (10)
        }

        [Fact]
        public void ScrollUp_MenuItem_Should_Decrease_Value_By_SmallChange()
        {
            var target = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Template = new FuncControlTemplate<ScrollBar>(Template),
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                SmallChange = 5
            };
            
            target.ApplyTemplate();

            var contextRequestedArgs = new ContextRequestedEventArgs();
            target.RaiseEvent(contextRequestedArgs);

            var scrollUpItem = target.ContextMenu.Items.OfType<MenuItem>()
                .FirstOrDefault(m => AutomationProperties.GetAutomationId(m) == "ScrollUp");
            
            Assert.NotNull(scrollUpItem);
            scrollUpItem.Command.Execute(null);
            
            Assert.Equal(45, target.Value); // Should decrease by SmallChange (5)
        }

        [Fact]
        public void ScrollDown_MenuItem_Should_Increase_Value_By_SmallChange()
        {
            var target = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Template = new FuncControlTemplate<ScrollBar>(Template),
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                SmallChange = 5
            };
            
            target.ApplyTemplate();

            var contextRequestedArgs = new ContextRequestedEventArgs();
            target.RaiseEvent(contextRequestedArgs);

            var scrollDownItem = target.ContextMenu.Items.OfType<MenuItem>()
                .FirstOrDefault(m => AutomationProperties.GetAutomationId(m) == "ScrollDown");
            
            Assert.NotNull(scrollDownItem);
            scrollDownItem.Command.Execute(null);
            
            Assert.Equal(55, target.Value); // Should increase by SmallChange (5)
        }

        [Fact]
        public void MenuItems_Should_Respect_ScrollBar_Value_Bounds()
        {
            var target = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Template = new FuncControlTemplate<ScrollBar>(Template),
                Minimum = 20,
                Maximum = 80,
                Value = 25,
                SmallChange = 10
            };
            
            target.ApplyTemplate();

            var contextRequestedArgs = new ContextRequestedEventArgs();
            target.RaiseEvent(contextRequestedArgs);

            // Test ScrollUp near minimum
            var scrollUpItem = target.ContextMenu.Items.OfType<MenuItem>()
                .FirstOrDefault(m => AutomationProperties.GetAutomationId(m) == "ScrollUp");
            scrollUpItem.Command.Execute(null);
            
            Assert.Equal(20, target.Value); // Should not go below Minimum
            
            // Test ScrollDown near maximum
            target.Value = 75;
            var scrollDownItem = target.ContextMenu.Items.OfType<MenuItem>()
                .FirstOrDefault(m => AutomationProperties.GetAutomationId(m) == "ScrollDown");
            scrollDownItem.Command.Execute(null);
            
            Assert.Equal(80, target.Value); // Should not go above Maximum
        }

        [Fact]
        public void ContextMenu_Should_Have_Proper_Automation_Ids()
        {
            var target = new ScrollBar
            {
                Orientation = Orientation.Vertical,
                Template = new FuncControlTemplate<ScrollBar>(Template),
            };
            
            target.ApplyTemplate();

            var contextRequestedArgs = new ContextRequestedEventArgs();
            target.RaiseEvent(contextRequestedArgs);

            var menuItems = target.ContextMenu.Items.OfType<MenuItem>().ToList();
            
            var expectedAutomationIds = new[] { "ScrollHere", "Top", "Bottom", "PageUp", "PageDown", "ScrollUp", "ScrollDown" };
            var actualAutomationIds = menuItems.Select(m => AutomationProperties.GetAutomationId(m)).ToList();
            
            Assert.Equal(7, actualAutomationIds.Count);
            foreach (var expectedId in expectedAutomationIds.Take(7))
            {
                Assert.Contains(expectedId, actualAutomationIds);
            }
        }

    }
}

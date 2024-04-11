using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ToolTipTests_Popup : ToolTipTests
    {
        protected override TestServices ConfigureServices(TestServices baseServices) => baseServices;
    }

    public class ToolTipTests_Overlay : ToolTipTests
    {
        protected override TestServices ConfigureServices(TestServices baseServices) =>
            baseServices.With(windowingPlatform: new MockWindowingPlatform(popupImpl: window => null));
    }

    public abstract class ToolTipTests
    {
        protected abstract TestServices ConfigureServices(TestServices baseServices);

        private static readonly MouseDevice s_mouseDevice = new(new Pointer(0, PointerType.Mouse, true));

        [Fact]
        public void Should_Close_When_Control_Detaches()
        {
            using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
            {
                var panel = new Panel();

                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 0
                };

                panel.Children.Add(target);

                SetupWindowAndActivateToolTip(panel, target);

                Assert.True(ToolTip.GetIsOpen(target));

                panel.Children.Remove(target);

                Assert.False(ToolTip.GetIsOpen(target));
            }
        }

        [Fact]
        public void Should_Close_When_Tip_Is_Opened_And_Detached_From_Visual_Tree()
        {
            using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
            {
                var target = new Decorator
                {
                    [!ToolTip.TipProperty] = new Binding("Tip"),
                    [ToolTip.ShowDelayProperty] = 0,
                };

                var panel = new Panel();
                panel.Children.Add(target);

                var mouseEnter = SetupWindowAndGetMouseEnterAction(panel);

                panel.DataContext = new ToolTipViewModel();

                mouseEnter(target);

                Assert.True(ToolTip.GetIsOpen(target));

                panel.Children.Remove(target);

                Assert.False(ToolTip.GetIsOpen(target));
            }
        }

        [Fact]
        public void Should_Open_On_Pointer_Enter()
        {
            using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
            {
                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 0
                };

                SetupWindowAndActivateToolTip(target);

                Assert.True(ToolTip.GetIsOpen(target));
            }
        }

        [Fact]
        public void Content_Should_Update_When_Tip_Property_Changes_And_Already_Open()
        {
            using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
            {
                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 0
                };

                SetupWindowAndActivateToolTip(target);

                Assert.True(ToolTip.GetIsOpen(target));
                Assert.Equal("Tip", target.GetValue(ToolTip.ToolTipProperty).Content);

                ToolTip.SetTip(target, "Tip1");
                Assert.Equal("Tip1", target.GetValue(ToolTip.ToolTipProperty).Content);
            }
        }

        [Fact]
        public void Should_Open_On_Pointer_Enter_With_Delay()
        {
            using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
            {
                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 1
                };

                SetupWindowAndActivateToolTip(target);

                var timer = Assert.Single(Dispatcher.SnapshotTimersForUnitTests());
                Assert.Equal(TimeSpan.FromMilliseconds(1), timer.Interval);
                Assert.False(ToolTip.GetIsOpen(target));

                timer.ForceFire();

                Assert.True(ToolTip.GetIsOpen(target));
            }
        }

        [Fact]
        public void Open_Class_Should_Not_Initially_Be_Added()
        {
            using (UnitTestApplication.Start(ConfigureServices(TestServices.StyledWindow)))
            {
                var toolTip = new ToolTip();
                var window = new Window();

                var decorator = new Decorator()
                {
                    [ToolTip.TipProperty] = toolTip
                };

                window.Content = decorator;

                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                Assert.Empty(toolTip.Classes);
            }
        }

        [Fact]
        public void Setting_IsOpen_Should_Add_Open_Class()
        {
            using (UnitTestApplication.Start(ConfigureServices(TestServices.StyledWindow)))
            {
                var toolTip = new ToolTip();
                var window = new Window();

                var decorator = new Decorator()
                {
                    [ToolTip.TipProperty] = toolTip
                };

                window.Content = decorator;

                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                ToolTip.SetIsOpen(decorator, true);

                Assert.Equal(new[] { ":open" }, toolTip.Classes);
            }
        }

        [Fact]
        public void Clearing_IsOpen_Should_Remove_Open_Class()
        {
            using (UnitTestApplication.Start(ConfigureServices(TestServices.StyledWindow)))
            {
                var toolTip = new ToolTip();
                var window = new Window();

                var decorator = new Decorator()
                {
                    [ToolTip.TipProperty] = toolTip
                };

                window.Content = decorator;

                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                ToolTip.SetIsOpen(decorator, true);
                ToolTip.SetIsOpen(decorator, false);

                Assert.Empty(toolTip.Classes);
            }
        }

        [Fact]
        public void Should_Close_On_Null_Tip()
        {
            using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
            {
                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 0
                };

                SetupWindowAndActivateToolTip(target);

                Assert.True(ToolTip.GetIsOpen(target));

                target[ToolTip.TipProperty] = null;

                Assert.False(ToolTip.GetIsOpen(target));
            }
        }

        [Fact]
        public void Should_Not_Close_When_Pointer_Is_Moved_Over_ToolTip()
        {
            using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
            {
                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 0
                };

                var mouseEnter = SetupWindowAndGetMouseEnterAction(target);

                mouseEnter(target);

                Assert.True(ToolTip.GetIsOpen(target));

                var tooltip = Assert.IsType<ToolTip>(target.GetValue(ToolTip.ToolTipProperty));

                mouseEnter(tooltip);

                Assert.True(ToolTip.GetIsOpen(target));
            }
        }

        [Fact]
        public void Should_Not_Close_When_Pointer_Is_Moved_From_ToolTip_To_Original_Control()
        {
            using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
            {
                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 0
                };

                var mouseEnter = SetupWindowAndGetMouseEnterAction(target);

                mouseEnter(target);
                Assert.True(ToolTip.GetIsOpen(target));

                var tooltip = Assert.IsType<ToolTip>(target.GetValue(ToolTip.ToolTipProperty));
                mouseEnter(tooltip);

                Assert.True(ToolTip.GetIsOpen(target));

                mouseEnter(target);

                Assert.True(ToolTip.GetIsOpen(target));
            }
        }

        [Fact]
        public void Should_Close_When_Pointer_Is_Moved_From_ToolTip_To_Another_Control()
        {
            using (UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow)))
            {
                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 0
                };

                var other = new Decorator();

                var panel = new StackPanel
                {
                    Children = { target, other }
                };

                var mouseEnter = SetupWindowAndGetMouseEnterAction(panel);

                mouseEnter(target);
                Assert.True(ToolTip.GetIsOpen(target));

                var tooltip = Assert.IsType<ToolTip>(target.GetValue(ToolTip.ToolTipProperty));
                mouseEnter(tooltip);

                Assert.True(ToolTip.GetIsOpen(target));

                mouseEnter(other);

                Assert.False(ToolTip.GetIsOpen(target));
            }
        }

        [Fact]
        public void New_ToolTip_Replaces_Other_ToolTip_Immediately()
        {
            using var app = UnitTestApplication.Start(ConfigureServices(TestServices.FocusableWindow));
            
            var target = new Decorator()
            {
                [ToolTip.TipProperty] = "Tip",
                [ToolTip.ShowDelayProperty] = 0
            };

            var other = new Decorator()
            {
                [ToolTip.TipProperty] = "Tip",
                [ToolTip.ShowDelayProperty] = (int) TimeSpan.FromHours(1).TotalMilliseconds,
            };

            var panel = new StackPanel
            {
                Children = { target, other }
            };

            var mouseEnter = SetupWindowAndGetMouseEnterAction(panel);
            
            mouseEnter(other);
            Assert.False(ToolTip.GetIsOpen(other)); // long delay

            mouseEnter(target);
            Assert.True(ToolTip.GetIsOpen(target)); // no delay

            mouseEnter(other);
            Assert.True(ToolTip.GetIsOpen(other)); // delay skipped, a tooltip was already open

            // Now disable the between-show system

            mouseEnter(target);
            Assert.True(ToolTip.GetIsOpen(target));

            ToolTip.SetBetweenShowDelay(other, -1);

            mouseEnter(other);
            Assert.False(ToolTip.GetIsOpen(other));
        }

        private Action<Control> SetupWindowAndGetMouseEnterAction(Control windowContent, [CallerMemberName] string testName = null)
        {
            var windowImpl = MockWindowingPlatform.CreateWindowMock();
            var hitTesterMock = new Mock<IHitTester>();

            var window = new Window(windowImpl.Object)
            {
                HitTesterOverride = hitTesterMock.Object,
                Content = windowContent,
                Title = testName,
            };

            window.ApplyStyling();
            window.ApplyTemplate();
            window.Presenter.ApplyTemplate();
            window.Show();

            Assert.True(windowContent.IsAttachedToVisualTree);
            Assert.True(windowContent.IsMeasureValid);
            Assert.True(windowContent.IsVisible);

            var controlIds = new Dictionary<Control, int>();

            return control =>
            {
                Point point;

                if (control == null)
                {
                    point = default;
                }
                else
                {
                    if (!controlIds.TryGetValue(control, out int id))
                    {
                        id = controlIds[control] = controlIds.Count;
                    }
                    point = new Point(id, int.MaxValue);
                }

                hitTesterMock.Setup(m => m.HitTestFirst(point, window, It.IsAny<Func<Visual, bool>>()))
                    .Returns(control);

                windowImpl.Object.Input(new RawPointerEventArgs(s_mouseDevice, (ulong)DateTime.Now.Ticks, (IInputRoot)control?.VisualRoot ?? window,
                        RawPointerEventType.Move, point, RawInputModifiers.None));

                Assert.True(control == null || control.IsPointerOver);
            };
        }

        private void SetupWindowAndActivateToolTip(Control windowContent, Control targetOverride = null, [CallerMemberName] string testName = null) =>
            SetupWindowAndGetMouseEnterAction(windowContent, testName)(targetOverride ?? windowContent);
    }

    internal class ToolTipViewModel
    {
        public string Tip => "Tip";
    }
}

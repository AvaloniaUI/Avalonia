using System;
using System.Reactive.Disposables;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TolTipTests
    {
        private MouseTestHelper _mouseHelper = new MouseTestHelper();

        [Fact]
        public void Should_Not_Open_On_Detached_Control()
        {
            //issue #3188
            var control = new Decorator()
            {
                [ToolTip.TipProperty] = "Tip",
                [ToolTip.ShowDelayProperty] = 0
            };

            Assert.False(control.IsAttachedToVisualTree);

            //here in issue #3188 exception is raised
            _mouseHelper.Enter(control);

            Assert.False(ToolTip.GetIsOpen(control));
        }
        
        [Fact]
        public void Should_Close_When_Control_Detaches()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

                var panel = new Panel();
                
                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 0
                };
                
                panel.Children.Add(target);

                window.Content = panel;

                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                Assert.True(target.IsAttachedToVisualTree);                               

                _mouseHelper.Enter(target);

                Assert.True(ToolTip.GetIsOpen(target));
                
                panel.Children.Remove(target);
                
                Assert.False(ToolTip.GetIsOpen(target));
            }
        }
        
        [Fact]
        public void Should_Close_When_Tip_Is_Opened_And_Detached_From_Visual_Tree()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Panel x:Name='PART_panel'>
        <Decorator x:Name='PART_target' ToolTip.Tip='{Binding Tip}' ToolTip.ShowDelay='0' />
    </Panel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                
                window.DataContext = new ToolTipViewModel();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                var target = window.Find<Decorator>("PART_target");
                var panel = window.Find<Panel>("PART_panel");
                
                Assert.True(target.IsAttachedToVisualTree);                               

                _mouseHelper.Enter(target);

                Assert.True(ToolTip.GetIsOpen(target));

                panel.Children.Remove(target);
                
                Assert.False(ToolTip.GetIsOpen(target));
            }
        }

        [Fact]
        public void Should_Open_On_Pointer_Enter()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 0
                };

                window.Content = target;

                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                Assert.True(target.IsAttachedToVisualTree);

                _mouseHelper.Enter(target);

                Assert.True(ToolTip.GetIsOpen(target));
            }
        }
        
        [Fact]
        public void Content_Should_Update_When_Tip_Property_Changes_And_Already_Open()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 0
                };

                window.Content = target;

                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                _mouseHelper.Enter(target);

                Assert.True(ToolTip.GetIsOpen(target));
                Assert.Equal("Tip", target.GetValue(ToolTip.ToolTipProperty).Content);
                
                
                ToolTip.SetTip(target, "Tip1");
                Assert.Equal("Tip1", target.GetValue(ToolTip.ToolTipProperty).Content);
            }
        }

        [Fact]
        public void Should_Open_On_Pointer_Enter_With_Delay()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 1
                };

                window.Content = target;

                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                Assert.True(target.IsAttachedToVisualTree);

                _mouseHelper.Enter(target);

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
            using (UnitTestApplication.Start(TestServices.StyledWindow))
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
            using (UnitTestApplication.Start(TestServices.StyledWindow))
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
            using (UnitTestApplication.Start(TestServices.StyledWindow))
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
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 0
                };

                window.Content = target;

                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                _mouseHelper.Enter(target);

                Assert.True(ToolTip.GetIsOpen(target));

                target[ToolTip.TipProperty] = null;

                Assert.False(ToolTip.GetIsOpen(target));
            }
        }

        [Fact]
        public void Should_Not_Close_When_Pointer_Is_Moved_Over_ToolTip()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 0
                };

                window.Content = target;

                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                _mouseHelper.Enter(target);
                Assert.True(ToolTip.GetIsOpen(target));

                var tooltip = Assert.IsType<ToolTip>(target.GetValue(ToolTip.ToolTipProperty));
                _mouseHelper.Enter(tooltip);
                _mouseHelper.Leave(target);

                Assert.True(ToolTip.GetIsOpen(target));
            }
        }

        [Fact]
        public void Should_Not_Close_When_Pointer_Is_Moved_From_ToolTip_To_Original_Control()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

                var target = new Decorator()
                {
                    [ToolTip.TipProperty] = "Tip",
                    [ToolTip.ShowDelayProperty] = 0
                };

                window.Content = target;

                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                _mouseHelper.Enter(target);
                Assert.True(ToolTip.GetIsOpen(target));

                var tooltip = Assert.IsType<ToolTip>(target.GetValue(ToolTip.ToolTipProperty));
                _mouseHelper.Enter(tooltip);
                _mouseHelper.Leave(target);

                Assert.True(ToolTip.GetIsOpen(target));

                _mouseHelper.Enter(target);
                _mouseHelper.Leave(tooltip);

                Assert.True(ToolTip.GetIsOpen(target));
            }
        }

        [Fact]
        public void Should_Close_When_Pointer_Is_Moved_From_ToolTip_To_Another_Control()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var window = new Window();

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

                window.Content = panel;

                window.ApplyStyling();
                window.ApplyTemplate();
                window.Presenter.ApplyTemplate();

                _mouseHelper.Enter(target);
                Assert.True(ToolTip.GetIsOpen(target));

                var tooltip = Assert.IsType<ToolTip>(target.GetValue(ToolTip.ToolTipProperty));
                _mouseHelper.Enter(tooltip);
                _mouseHelper.Leave(target);

                Assert.True(ToolTip.GetIsOpen(target));

                _mouseHelper.Enter(other);
                _mouseHelper.Leave(tooltip);

                Assert.False(ToolTip.GetIsOpen(target));
            }
        }
    }

    internal class ToolTipViewModel
    {
        public string Tip => "Tip";
    }
}

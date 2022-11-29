using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Logging
{
    public class LoggingTests
    {
        [Fact]
        public void Control_Should_Not_Log_Binding_Errors_When_Detached_From_Visual_Tree()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Base.UnitTests.Logging;assembly=Avalonia.UnitTests'>
    <Panel Name='panel'>
    <Rectangle Name='rect' Fill='{Binding $parent[Window].Background}'/>
  </Panel>
</Window>";

                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var calledTimes = 0;
                using var logSink = TestLogSink.Start((l, a, s, m, d) =>
                {
                    if (l >= Avalonia.Logging.LogEventLevel.Warning)
                    {
                        calledTimes++;
                    }
                });
                var panel = window.FindControl<Panel>("panel");
                var rect = window.FindControl<Rectangle>("rect");
                window.ApplyTemplate();
                ((Control)window.Presenter).ApplyTemplate();
                panel.Children.Remove(rect);
                Assert.Equal(0, calledTimes);
            }
        }

        [Fact]
        public void Control_Should_Log_Binding_Errors_When_No_Ancestor_With_Such_Name()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Base.UnitTests.Logging;assembly=Avalonia.UnitTests'>
    <Panel>
    <Rectangle Fill='{Binding $parent[Grid].Background}'/>
  </Panel>
</Window>";
                var calledTimes = 0;
                using var logSink = TestLogSink.Start((l, a, s, m, d) =>
                {
                    if (l >= Avalonia.Logging.LogEventLevel.Warning && s is Rectangle)
                    {
                        calledTimes++;
                    }
                });
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                window.ApplyTemplate();
                ((Control)window.Presenter).ApplyTemplate();
                Assert.Equal(1, calledTimes);
            }
        }
    }


}

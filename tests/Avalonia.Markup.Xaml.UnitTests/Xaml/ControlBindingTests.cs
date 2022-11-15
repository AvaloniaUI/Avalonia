using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Logging;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class ControlBindingTests : XamlTestBase
    {
        [Fact]
        public void Binding_ProgressBar_Value_To_Invalid_Value_Uses_FallbackValue()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'>
    <ProgressBar Maximum='10' Value='{Binding Value, FallbackValue=3}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var progressBar = (ProgressBar)window.Content;

                window.DataContext = new { Value = "foo" };
                window.ApplyTemplate();

                Assert.Equal(3, progressBar.Value);
            }
        }

        [Fact]
        public void Invalid_FallbackValue_Logs_Error()
        {
            var called = false;

            LogCallback checkLogMessage = (level, area, src, mt, pv) =>
            {
                if (level == LogEventLevel.Warning &&
                    area == LogArea.Binding &&
                    mt == "Error in binding to {Target}.{Property}: {Message}" &&
                    pv.Length == 3 &&
                    pv[0] is ProgressBar &&
                    object.ReferenceEquals(pv[1], ProgressBar.ValueProperty) &&
                    (string)pv[2] == "Could not convert FallbackValue 'bar' to 'System.Double'")
                {
                    called = true;
                }
            };

            using (UnitTestApplication.Start(TestServices.StyledWindow))
            using (TestLogSink.Start(checkLogMessage))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'>
    <ProgressBar Maximum='10' Value='{Binding Value, FallbackValue=bar}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var progressBar = (ProgressBar)window.Content;

                window.DataContext = new { Value = "foo" };
                window.ApplyTemplate();

                Assert.Equal(0, progressBar.Value);
                Assert.True(called);
            }
        }

        [Fact]
        public void Can_Bind_Between_TabStrip_And_Carousel()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'>
    <DockPanel>
        <TabStrip Name='strip' DockPanel.Dock='Top' Items='{Binding Items}' SelectedIndex='0'>
          <TabStrip.ItemTemplate>
            <DataTemplate>
              <TextBlock Text='{Binding Header}'/>
            </DataTemplate>
          </TabStrip.ItemTemplate>
        </TabStrip>
        <Carousel Name='carousel' Items='{Binding Items}' SelectedIndex='{Binding #strip.SelectedIndex}'>
          <Carousel.ItemTemplate>
            <DataTemplate>
              <TextBlock Text='{Binding Detail}'/>
            </DataTemplate>
          </Carousel.ItemTemplate>
        </Carousel>
    </DockPanel>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var strip = window.FindControl<TabStrip>("strip");
                var carousel = window.FindControl<Carousel>("carousel");

                window.DataContext = new ItemsViewModel
                {
                    Items = new[]
                    {
                        new ItemViewModel { Header = "Item1", Detail = "Detail1" },
                        new ItemViewModel { Header = "Item2", Detail = "Detail2" },
                    }
                };

                window.Show();

                Assert.Equal(0, strip.SelectedIndex);
                Assert.Equal(0, carousel.SelectedIndex);
            }
        }

        private class ItemsViewModel
        {
            public IList<ItemViewModel> Items { get; set; }
        }

        private class ItemViewModel
        {
            public string Header { get; set; }
            public string Detail { get; set; }
        }
    }
}

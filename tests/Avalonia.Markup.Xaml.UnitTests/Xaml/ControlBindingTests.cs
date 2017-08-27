// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class ControlBindingTests
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
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
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var progressBar = (ProgressBar)window.Content;

                window.DataContext = new { Value = "foo" };
                window.ApplyTemplate();

                Assert.Equal(0, progressBar.Value);
                Assert.True(called);
            }
        }
    }
}

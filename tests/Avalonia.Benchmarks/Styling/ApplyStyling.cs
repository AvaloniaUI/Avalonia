using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;

namespace Avalonia.Benchmarks.Styling
{
    [MemoryDiagnoser]
    public class ApplyStyling : IDisposable
    {
        private IDisposable _app;
        private Window _window;

        public ApplyStyling()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            TextBox textBox;

            _window = new Window
            {
                Content = textBox = new TextBox(),
            };

            _window.ApplyTemplate();
            textBox.ApplyTemplate();

            var border = (Border)textBox.GetVisualChildren().Single();

            if (border.BorderThickness != new Thickness(2))
            {
                throw new Exception("Styles not applied.");
            }

            _window.Content = null;

            // Add a bunch of styles with lots of class selectors to complicate matters.
            for (int i = 0; i < 100; ++i)
            {
                _window.Styles.Add(new Style(x => x.OfType<TextBox>().Class("foo").Class("bar").Class("baz"))
                {
                    Setters =
                    {
                        new Setter(TextBox.TextProperty, "foo"),
                    }
                });
            }
        }

        public void Dispose()
        {
            _app.Dispose();
        }

        [Benchmark]
        public void Add_And_Style_TextBox()
        {
            var textBox = new TextBox();
            _window.Content = textBox;
            textBox.ApplyTemplate();
        }
    }
}

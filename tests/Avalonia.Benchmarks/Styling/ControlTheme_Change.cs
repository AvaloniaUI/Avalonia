using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Styling
{
    [MemoryDiagnoser]
    public class ControlTheme_Change : IDisposable
    {
        private readonly IDisposable _app;
        private readonly TestRoot _root;
        private readonly TextBox _control;
        private readonly ControlTheme _theme1;
        private readonly ControlTheme _theme2;

        public ControlTheme_Change()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            // Simulate an application with a lot of styles by creating a tree of nested panels,
            // each with a bunch of styles applied.
            var (rootPanel, leafPanel) = CreateNestedPanels(10);

            // We're benchmarking how long it takes to switch control theme on a TextBox in this
            // situation.
            var baseTheme = (ControlTheme)Application.Current.FindResource(typeof(TextBox)) ??
                throw new Exception("Base TextBox theme not found.");

            _theme1 = new ControlTheme(typeof(TextBox))
            {
                BasedOn = baseTheme,
                Setters = { new Setter(TextBox.BackgroundProperty, Brushes.Red) },
            };

            _theme2 = new ControlTheme(typeof(TextBox))
            {
                BasedOn = baseTheme,
                Setters = { new Setter(TextBox.BackgroundProperty, Brushes.Green) },
            };

            _control = new TextBox { Theme = _theme1 };
            leafPanel.Children.Add(_control);

            _root = new TestRoot(true, rootPanel)
            {
                Renderer = new NullRenderer(),
            };

            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Change_ControlTheme()
        {
            if (_control.Background != Brushes.Red)
                throw new Exception("Invalid benchmark state");

            _control.Theme = _theme2;
            _root.LayoutManager.ExecuteLayoutPass();

            if (_control.Background != Brushes.Green)
                throw new Exception("Invalid benchmark state");

            _control.Theme = _theme1;
            _root.LayoutManager.ExecuteLayoutPass();

            if (_control.Background != Brushes.Red)
                throw new Exception("Invalid benchmark state");
        }

        public void Dispose()
        {
            _app.Dispose();
        }

        private static (Panel, Panel) CreateNestedPanels(int count)
        {
            var root = new Panel();
            var last = root;

            for (var i = 0; i < count; ++i)
            {
                var panel = new Panel();
                panel.Styles.AddRange(CreateStyles());
                last.Children.Add(panel);
                last = panel;
            }

            return (root, last);
        }

        private static IEnumerable<IStyle> CreateStyles()
        {
            var types = new[]
            {
                typeof(Border),
                typeof(Button),
                typeof(ButtonSpinner),
                typeof(Carousel),
                typeof(CheckBox),
                typeof(ComboBox),
                typeof(ContentControl),
                typeof(Expander),
                typeof(ItemsControl),
                typeof(Label),
                typeof(ListBox),
                typeof(ProgressBar),
                typeof(RadioButton),
                typeof(RepeatButton),
                typeof(ScrollViewer),
                typeof(Slider),
                typeof(Spinner),
                typeof(SplitView),
                typeof(TextBox),
                typeof(ToggleSwitch),
                typeof(TreeView),
                typeof(Viewbox),
                typeof(Window),
            };

            foreach (var type in types)
            {
                yield return new Style(x => x.OfType(type))
                {
                    Setters = { new Setter(Control.TagProperty, type.Name) }
                };

                yield return new Style(x => x.OfType(type).Class("foo"))
                {
                    Setters = { new Setter(Control.TagProperty, type.Name + " foo") }
                };

                yield return new Style(x => x.OfType(type).Class("bar"))
                {
                    Setters = { new Setter(Control.TagProperty, type.Name + " bar") }
                };
            }
        }
    }
}

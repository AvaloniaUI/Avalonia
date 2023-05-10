using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Styling
{
    [MemoryDiagnoser]
    public class Style_Apply_Detach_Complex : IDisposable
    {
        private readonly IDisposable _app;
        private readonly TestRoot _root;
        private readonly TextBox _control;

        public Style_Apply_Detach_Complex()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);

            // Simulate an application with a lot of styles by creating a tree of nested panels,
            // each with a bunch of styles applied.
            var (rootPanel, leafPanel) = CreateNestedPanels(10);

            // We're benchmarking how long it takes to apply styles to a TextBox in this situation.
            _control = new TextBox();
            leafPanel.Children.Add(_control);

            _root = new TestRoot(true, rootPanel)
            {
                Renderer = new NullRenderer(),
            };
        }

        [Benchmark]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Apply_Detach_Styles()
        {
            // Styles will have already been attached when attached to the logical tree, so remove
            // the styles first.
            if ((string)_control.Tag != "TextBox")
                throw new Exception("Invalid benchmark state");

            _control.InvalidateStyles(true);

            if (_control.Tag is not null)
                throw new Exception("Invalid benchmark state");

            // Then re-apply the styles.
            _control.ApplyStyling();
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

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using BenchmarkDotNet.Attributes;

#nullable enable

namespace Avalonia.Benchmarks.Styling
{
    [MemoryDiagnoser]
    public class ControlTheme_Apply
    {
        private ControlTheme _theme;
        private ControlTheme _otherTheme;
        private List<Style> _styles = new();

        public ControlTheme_Apply()
        {
            RuntimeHelpers.RunClassConstructor(typeof(TestControl).TypeHandle);

            _theme = CreateControlTheme(Brushes.Red);
            _otherTheme = CreateControlTheme(Brushes.Orange);

            for (var i = 0; i < 100; ++i)
            {
                _styles.Add(new Style(x => x.OfType<TestControl>())
                {
                    Setters = { new Setter(TestControl.BackgroundProperty, Brushes.Yellow) }
                });
            }
        }

        [Benchmark]
        public void Apply_Control_Theme()
        {
            var target = new TestControl();

            target.BeginBatchUpdate();

            _theme.TryAttach(target, null);
            target.ApplyTemplate();
            _theme.TryAttach(target.VisualChild, null);

            target.EndBatchUpdate();
        }


        [Benchmark]
        public void Apply_Remove_Control_Theme()
        {
            var target = new TestControl();

            target.BeginBatchUpdate();

            _theme.TryAttach(target, null);
            target.ApplyTemplate();
            _theme.TryAttach(target.VisualChild, null);

            target.EndBatchUpdate();

            // Switching to another theme will cause the current theme to be removed but won't
            // immediately apply the new theme, so for the benefit of the benchmark it has the
            // effect of simply removing the theme.
            target.Theme = _otherTheme;
        }

        [Benchmark]
        public void Apply_Control_Theme_With_Styles()
        {
            var target = new TestControl();

            target.BeginBatchUpdate();

            _theme.TryAttach(target, null);
            target.ApplyTemplate();
            _theme.TryAttach(target.VisualChild, null);

            foreach (var style in _styles)
                style.TryAttach(target, null);

            target.EndBatchUpdate();
        }

        [Benchmark]
        public void Apply_Remove_Control_Theme_With_Styles()
        {
            var target = new TestControl();

            target.BeginBatchUpdate();

            _theme.TryAttach(target, null);
            target.ApplyTemplate();
            _theme.TryAttach(target.VisualChild, null);

            foreach (var style in _styles)
                style.TryAttach(target, null);

            target.EndBatchUpdate();

            // Switching to another theme will cause the current theme to be removed but won't
            // immediately apply the new theme, so for the benefit of the benchmark it has the
            // effect of simply removing the theme.
            target.Theme = _otherTheme;
        }

        private static ControlTheme CreateControlTheme(IBrush background)
        {
            return new ControlTheme(typeof(TestControl))
            {
                Setters =
                {
                    new Setter(TestControl.BackgroundProperty, Brushes.Transparent),
                    new Setter(TestControl.TemplateProperty, new FuncControlTemplate<TestControl>((_, x) =>
                        new Border())),
                },
                Children =
                {
                    new Style(x => x.Nesting().Template().OfType<Border>())
                    {
                        Setters = { new Setter(TestControl.BackgroundProperty, Brushes.Red), }
                    },
                    new Style(x => x.Nesting().Class(":pointerover").Template().OfType<Border>())
                    {
                        Setters = { new Setter(TestControl.BackgroundProperty, Brushes.Green), }
                    },
                    new Style(x => x.Nesting().Class(":pressed").Template().OfType<Border>())
                    {
                        Setters = { new Setter(TestControl.BackgroundProperty, Brushes.Blue), }
                    },
                }
            };
        }

        private class TestControl : TemplatedControl
        {
            public IStyleable VisualChild => (IStyleable)VisualChildren[0];
        }

        private class TestClass2 : Control
        {
        }
    }
}

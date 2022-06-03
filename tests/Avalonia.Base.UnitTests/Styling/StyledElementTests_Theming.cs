using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.Styling;

public class StyledElementTests_Theming
{
    public class InlineTheme
    {
        [Fact]
        public void Theme_Is_Applied_When_Attached_To_Logical_Tree()
        {
            using var app = UnitTestApplication.Start(TestServices.RealStyler);
            var target = CreateTarget();

            Assert.Null(target.Template);

            var root = CreateRoot(target);
            Assert.NotNull(target.Template);

            var border = Assert.IsType<Border>(target.VisualChild);
            Assert.Equal(Brushes.Red, border.Background);

            target.Classes.Add("foo");
            Assert.Equal(Brushes.Green, border.Background);
        }

        [Fact]
        public void Theme_Is_Detached_When_Theme_Property_Cleared()
        {
            using var app = UnitTestApplication.Start(TestServices.RealStyler);
            var target = CreateTarget();
            var root = CreateRoot(target);

            Assert.NotNull(target.Template);

            target.Theme = null;
            Assert.Null(target.Template);
        }

        [Fact]
        public void Theme_Is_Detached_From_Template_Controls_When_Theme_Property_Cleared()
        {
            using var app = UnitTestApplication.Start(TestServices.RealStyler);

            var theme = new ControlTheme
            {
                TargetType = typeof(ThemedControl),
                Children =
                {
                    new Style(x => x.Nesting().Template().OfType<Canvas>())
                    {
                        Setters =
                        {
                            new Setter(Canvas.BackgroundProperty, Brushes.Red),
                        }
                    },
                }
            };

            var target = CreateTarget(theme);
            target.Template = new FuncControlTemplate<ThemedControl>((o, n) => new Canvas());

            var root = CreateRoot(target);

            var canvas = Assert.IsType<Canvas>(target.VisualChild);
            Assert.Equal(Brushes.Red, canvas.Background);

            target.Theme = null;

            Assert.IsType<Canvas>(target.VisualChild);
            Assert.Null(canvas.Background);
        }

        [Fact]
        public void Theme_Is_Applied_On_Layout_After_Theme_Property_Changes()
        {
            using var app = UnitTestApplication.Start(TestServices.RealStyler);
            var target = new ThemedControl();
            var root = CreateRoot(target);

            Assert.Null(target.Template);

            target.Theme = CreateTheme();
            Assert.Null(target.Template);

            root.LayoutManager.ExecuteLayoutPass();

            var border = Assert.IsType<Border>(target.VisualChild);
            Assert.NotNull(target.Template);
            Assert.Equal(Brushes.Red, border.Background);
        }

        [Fact]
        public void BasedOn_Theme_Is_Applied_When_Attached_To_Logical_Tree()
        {
            using var app = UnitTestApplication.Start(TestServices.RealStyler);
            var target = CreateTarget(CreateDerivedTheme());

            Assert.Null(target.Template);

            var root = CreateRoot(target);
            Assert.NotNull(target.Template);
            Assert.Equal(Brushes.Blue, target.BorderBrush);

            var border = Assert.IsType<Border>(target.VisualChild);
            Assert.Equal(Brushes.Red, border.Background);
            Assert.Equal(Brushes.Yellow, border.BorderBrush);

            target.Classes.Add("foo");
            Assert.Equal(Brushes.Green, border.Background);
            Assert.Equal(Brushes.Cyan, border.BorderBrush);
        }

        private static ThemedControl CreateTarget(ControlTheme? theme = null)
        {
            return new ThemedControl
            {
                Theme = theme ?? CreateTheme(),
            };
        }

        private static TestRoot CreateRoot(IControl child)
        {
            var result = new TestRoot(child);
            result.LayoutManager.ExecuteInitialLayoutPass();
            return result;
        }
    }

    public class ThemeFromStyle
    {
        [Fact]
        public void Theme_Is_Applied_When_Attached_To_Logical_Tree()
        {
            using var app = UnitTestApplication.Start(TestServices.RealStyler);
            var target = CreateTarget();

            Assert.Null(target.Theme);
            Assert.Null(target.Template);

            var root = CreateRoot(target);

            Assert.NotNull(target.Theme);
            Assert.NotNull(target.Template);

            var border = Assert.IsType<Border>(target.VisualChild);
            Assert.Equal(border.Background, Brushes.Red);

            target.Classes.Add("foo");
            Assert.Equal(border.Background, Brushes.Green);
        }

        private static ThemedControl CreateTarget()
        {
            return new ThemedControl();
        }

        private static TestRoot CreateRoot(IControl child)
        {
            var result = new TestRoot()
            {
                Styles =
            {
                new Style(x => x.OfType<ThemedControl>())
                {
                    Setters =
                    {
                        new Setter(TemplatedControl.ThemeProperty, CreateTheme())
                    }
                }
            }
            };

            result.Child = child;
            result.LayoutManager.ExecuteInitialLayoutPass();
            return result;
        }
    }

    private static ControlTheme CreateTheme()
    {
        var template = new FuncControlTemplate<ThemedControl>((o, n) => new Border());

        return new ControlTheme
        {
            TargetType = typeof(ThemedControl),
            Setters =
            {
                new Setter(ThemedControl.TemplateProperty, template),
            },
            Children =
            {
                new Style(x => x.Nesting().Template().OfType<Border>())
                {
                    Setters =
                    {
                        new Setter(Border.BackgroundProperty, Brushes.Red),
                    }
                },
                new Style(x => x.Nesting().Class("foo").Template().OfType<Border>())
                {
                    Setters =
                    {
                        new Setter(Border.BackgroundProperty, Brushes.Green),
                    }
                },
            }
        };
    }

    private static ControlTheme CreateDerivedTheme()
    {
        return new ControlTheme
        {
            TargetType = typeof(ThemedControl),
            BasedOn = CreateTheme(),
            Setters =
            {
                new Setter(Border.BorderBrushProperty, Brushes.Blue),
            },
            Children =
            {
                new Style(x => x.Nesting().Template().OfType<Border>())
                {
                    Setters =
                    {
                        new Setter(Border.BorderBrushProperty, Brushes.Yellow),
                    }
                },
                new Style(x => x.Nesting().Class("foo").Template().OfType<Border>())
                {
                    Setters =
                    {
                        new Setter(Border.BorderBrushProperty, Brushes.Cyan),
                    }
                },
            }
        };
    }

    private class ThemedControl : TemplatedControl
    {
        public IVisual? VisualChild => VisualChildren?.SingleOrDefault();
    }
}

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

public class StyledElementTests_Theming : ScopedTestBase
{
    public class InlineTheme : ScopedTestBase
    {
        [Fact]
        public void Theme_Is_Applied_When_Attached_To_Logical_Tree()
        {
            var target = CreateTarget();

            Assert.Null(target.Template);

            CreateRoot(target);
            Assert.NotNull(target.Template);

            var border = Assert.IsType<Border>(target.VisualChild);
            Assert.Equal(Brushes.Red, border.Background);

            target.Classes.Add("foo");
            Assert.Equal(Brushes.Green, border.Background);
        }

        [Fact]
        public void Theme_Is_Applied_To_Derived_Class_When_Attached_To_Logical_Tree()
        {
            var target = new DerivedThemedControl
            {
                Theme = CreateTheme(),
            };

            Assert.Null(target.Template);

            CreateRoot(target);
            Assert.NotNull(target.Template);

            var border = Assert.IsType<Border>(target.VisualChild);
            Assert.Equal(Brushes.Red, border.Background);

            target.Classes.Add("foo");
            Assert.Equal(Brushes.Green, border.Background);
        }

        [Fact]
        public void Theme_Is_Detached_When_Theme_Property_Cleared()
        {
            var target = CreateTarget();
            CreateRoot(target);

            Assert.NotNull(target.Template);

            target.Theme = null;
            Assert.Null(target.Template);
        }

        [Fact]
        public void Setting_Explicit_Theme_Detaches_Default_Theme()
        {
            var target = new ThemedControl();
            var root = new TestRoot
            {
                Resources = { { typeof(ThemedControl), CreateTheme() } },
                Child = target,
            };

            root.LayoutManager.ExecuteInitialLayoutPass();

            Assert.Equal("theme", target.Tag);

            target.Theme = new ControlTheme(typeof(ThemedControl))
            {
                Setters =
                {
                    new Setter(ThemedControl.BackgroundProperty, Brushes.Yellow),
                }
            };

            root.LayoutManager.ExecuteLayoutPass();

            Assert.Null(target.Tag);
            Assert.Equal(Brushes.Yellow, target.Background);
        }

        [Fact]
        public void Unrelated_Styles_Are_Not_Detached_When_Theme_Property_Cleared()
        {
            var target = CreateTarget();
            CreateRoot(target, createAdditionalStyles: true);

            Assert.Equal("style", target.Tag);

            target.Theme = null;
            Assert.Equal("style", target.Tag);
        }

        [Fact]
        public void TemplatedParent_Theme_Is_Detached_From_Template_Controls_When_Theme_Property_Cleared()
        {
            var theme = new ControlTheme
            {
                TargetType = typeof(ThemedControl),
                Children =
                {
                    new Style(x => x.Nesting().Template().OfType<Canvas>())
                    {
                        Setters =
                        {
                            new Setter(Panel.BackgroundProperty, Brushes.Red),
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

            Assert.Same(canvas, target.VisualChild);
            Assert.Null(canvas.Background);
        }

        [Fact]
        public void Primary_Theme_Is_Not_Detached_From_Template_Controls_When_Theme_Property_Cleared()
        {
            var templatedParentTheme = new ControlTheme
            {
                TargetType = typeof(ThemedControl),
                Children =
                {
                    new Style(x => x.Nesting().Template().OfType<Button>())
                    {
                        Setters =
                        {
                            new Setter(Panel.BackgroundProperty, Brushes.Red),
                        }
                    },
                }
            };

            var childTheme = new ControlTheme
            {
                TargetType = typeof(Button),
                Setters =
                {
                    new Setter(TemplatedControl.ForegroundProperty, Brushes.Green),
                }
            };

            var target = CreateTarget(templatedParentTheme);
            target.Template = new FuncControlTemplate<ThemedControl>((o, n) => new Button
            {
                Theme = childTheme,
            });

            var root = CreateRoot(target, createAdditionalStyles: true);

            var templateChild = Assert.IsType<Button>(target.VisualChild);
            Assert.Equal(Brushes.Red, templateChild.Background);
            Assert.Equal(Brushes.Green, templateChild.Foreground);

            target.Theme = null;

            Assert.Null(templateChild.Background);
            Assert.Equal(Brushes.Green, templateChild.Foreground);
        }

        [Fact]
        public void TemplatedParent_Theme_Is_Not_Detached_From_Template_Controls_When_Primary_Theme_Property_Cleared()
        {
            var templatedParentTheme = new ControlTheme
            {
                TargetType = typeof(ThemedControl),
                Children =
                {
                    new Style(x => x.Nesting().Template().OfType<Button>())
                    {
                        Setters =
                        {
                            new Setter(Panel.BackgroundProperty, Brushes.Red),
                        }
                    },
                }
            };

            var childTheme = new ControlTheme
            {
                TargetType = typeof(Button),
                Setters =
                {
                    new Setter(Button.TagProperty, "childTheme"),
                }
            };

            var target = CreateTarget(templatedParentTheme);
            target.Template = new FuncControlTemplate<ThemedControl>((o, n) => new Button
            {
                Theme = childTheme,
            });

            var root = CreateRoot(target, createAdditionalStyles: true);

            var templateChild = Assert.IsType<Button>(target.VisualChild);
            Assert.Equal(Brushes.Red, templateChild.Background);
            Assert.Equal("childTheme", templateChild.Tag);

            templateChild.Theme = null;

            Assert.Equal(Brushes.Red, templateChild.Background);
            Assert.Null(templateChild.Tag);
        }

        [Fact]
        public void Unrelated_Styles_Are_Not_Detached_From_Template_Controls_When_Theme_Property_Cleared()
        {
            var target = CreateTarget();
            var root = CreateRoot(target, createAdditionalStyles: true);

            var canvas = Assert.IsType<Border>(target.VisualChild);
            Assert.Equal("style", canvas.Tag);

            target.Theme = null;

            Assert.Same(canvas, target.VisualChild);
            Assert.Equal("style", canvas.Tag);
        }

        [Fact]
        public void Theme_Is_Applied_On_Layout_After_Theme_Property_Changes()
        {
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
            var target = CreateTarget(CreateDerivedTheme());

            Assert.Null(target.Template);

            CreateRoot(target);
            Assert.NotNull(target.Template);
            Assert.Equal(Brushes.Blue, target.BorderBrush);

            var border = Assert.IsType<Border>(target.VisualChild);
            Assert.Equal(Brushes.Red, border.Background);
            Assert.Equal(Brushes.Yellow, border.BorderBrush);

            target.Classes.Add("foo");
            Assert.Equal(Brushes.Green, border.Background);
            Assert.Equal(Brushes.Cyan, border.BorderBrush);
        }

        [Fact]
        public void Theme_Has_Lower_Priority_Than_Style()
        {
            var target = CreateTarget();
            CreateRoot(target, createAdditionalStyles: true);

            Assert.Equal("style", target.Tag);
        }

        [Fact]
        public void Theme_Has_Lower_Priority_Than_Style_After_Change()
        {
            var target = CreateTarget();
            var theme = target.Theme;
            CreateRoot(target, createAdditionalStyles: true);

            target.Theme = null;
            target.Theme = theme;
            target.ApplyStyling();

            Assert.Equal("style", target.Tag);
        }

        private static ThemedControl CreateTarget(ControlTheme? theme = null)
        {
            return new ThemedControl
            {
                Theme = theme ?? CreateTheme(),
            };
        }

        private static TestRoot CreateRoot(
            Control child,
            bool createAdditionalStyles = false)
        {
            var result = new TestRoot();

            if (createAdditionalStyles)
            {
                result.Styles.Add(new Style(x => x.OfType<ThemedControl>())
                {
                    Setters =
                    {
                        new Setter(Control.TagProperty, "style"),
                    }
                });

                result.Styles.Add(new Style(x => x.OfType<Border>())
                {
                    Setters =
                    {
                        new Setter(Control.TagProperty, "style"),
                    }
                });
            }

            result.Child = child;
            result.LayoutManager.ExecuteInitialLayoutPass();
            return result;
        }
    }

    public class ImplicitTheme : ScopedTestBase
    {
        [Fact]
        public void Implicit_Theme_Is_Applied_When_Attached_To_Logical_Tree()
        {
            var target = CreateTarget();
            CreateRoot(target);
            Assert.NotNull(target.Template);

            var border = Assert.IsType<Border>(target.VisualChild);
            Assert.Equal(Brushes.Red, border.Background);

            target.Classes.Add("foo");
            Assert.Equal(Brushes.Green, border.Background);
        }

        [Fact]
        public void Implicit_Theme_Is_Not_Detached_When_Removed_From_Logical_Tree()
        {
            var target = CreateTarget();
            var root = CreateRoot(target);

            Assert.Equal("theme", target.Tag);

            root.Child = null;

            var border = Assert.IsType<Border>(target.VisualChild);
            Assert.Equal("theme", target.Tag);
            Assert.Equal("theme", border.Tag);
        }

        [Fact]
        public void Can_Attach_Then_Reattach_To_Same_Logical_Tree()
        {
            var target = CreateTarget();
            var root = CreateRoot(target);

            Assert.Equal("theme", target.Tag);

            root.Child = null;
            root.Child = target;

            Assert.Equal("theme", target.Tag);
        }

        [Fact]
        public void Implicit_Theme_Is_Reevaluated_When_Removed_And_Added_To_Different_Logical_Tree()
        {
            var target = CreateTarget();
            var root1 = CreateRoot(target, "theme1");
            var root2 = CreateRoot(null, "theme2");

            Assert.Equal("theme1", target.Tag);

            root1.Child = null;
            root2.Child = target;

            var border = Assert.IsType<Border>(target.VisualChild);
            Assert.Equal("theme2", target.Tag);
            Assert.Equal("theme2", border.Tag);
        }

        [Fact]
        public void Nested_Style_Can_Override_Property_In_Inner_Templated_Control()
        {
            var target = new ThemedControl2
            {
                Theme = new ControlTheme(typeof(ThemedControl2))
                {
                    Setters =
                    {
                        new Setter(
                            Controls.Primitives.TemplatedControl.TemplateProperty,
                            new FuncControlTemplate<ThemedControl2>((o, n) => new ThemedControl())),
                    },
                    Children =
                    {
                        new Style(x => x.Nesting().Template().OfType<ThemedControl>())
                        {
                            Setters = { new Setter(Controls.Primitives.TemplatedControl.CornerRadiusProperty, new CornerRadius(7)), }
                        },
                    }
                },
            };

            var root = CreateRoot(target);
            var inner = Assert.IsType<ThemedControl>(target.VisualChild);

            Assert.Equal(new CornerRadius(7), inner.CornerRadius);
        }

        private static ThemedControl CreateTarget() => new ThemedControl();

        private static TestRoot CreateRoot(Control? child, string themeTag = "theme")
        {
            var result = new TestRoot();
            result.Resources.Add(typeof(ThemedControl), CreateTheme(themeTag));
            result.Child = child;
            result.LayoutManager.ExecuteInitialLayoutPass();
            return result;
        }
    }

    public class ThemeFromStyle : ScopedTestBase
    {
        [Fact]
        public void Theme_Is_Applied_When_Attached_To_Logical_Tree()
        {
            var target = CreateTarget();

            Assert.Null(target.Theme);
            Assert.Null(target.Template);

            CreateRoot(target);

            Assert.NotNull(target.Theme);
            Assert.NotNull(target.Template);

            var border = Assert.IsType<Border>(target.VisualChild);
            Assert.Equal(Brushes.Red, border.Background);

            target.Classes.Add("foo");
            Assert.Equal(Brushes.Green, border.Background);
        }

        [Fact]
        public void Theme_Can_Be_Changed_By_Style_Class()
        {
            var target = CreateTarget();
            var theme1 = CreateTheme();
            var theme2 = new ControlTheme(typeof(ThemedControl));
            var root = new TestRoot()
            {
                Styles =
                {
                    new Style(x => x.OfType<ThemedControl>())
                    {
                        Setters = { new Setter(StyledElement.ThemeProperty, theme1) }
                    },
                    new Style(x => x.OfType<ThemedControl>().Class("bar"))
                    {
                        Setters = { new Setter(StyledElement.ThemeProperty, theme2) }
                    },
                }
            };

            root.Child = target;
            root.LayoutManager.ExecuteInitialLayoutPass();

            Assert.Same(theme1, target.Theme);
            Assert.NotNull(target.Template);

            target.Classes.Add("bar");
            Assert.Same(theme2, target.Theme);
            Assert.Null(target.Template);
        }

        [Fact]
        public void Theme_Can_Be_Set_To_LocalValue_While_Updating_Due_To_Style_Class()
        {
            var target = CreateTarget();
            var theme1 = CreateTheme();
            var theme2 = new ControlTheme(typeof(ThemedControl));
            var theme3 = new ControlTheme(typeof(ThemedControl));
            var root = new TestRoot()
            {
                Styles =
                {
                    new Style(x => x.OfType<ThemedControl>())
                    {
                        Setters = { new Setter(StyledElement.ThemeProperty, theme1) }
                    },
                    new Style(x => x.OfType<ThemedControl>().Class("bar"))
                    {
                        Setters = { new Setter(StyledElement.ThemeProperty, theme2) }
                    },
                }
            };

            root.Child = target;
            root.LayoutManager.ExecuteInitialLayoutPass();

            Assert.Same(theme1, target.Theme);
            Assert.NotNull(target.Template);

            target.Classes.Add("bar");

            // At this point, theme2 has been promoted to a local value internally in StyledElement;
            // make sure that setting a new local value here doesn't cause it to be cleared when we
            // do a layout pass because StyledElement thinks its clearing the promoted theme.
            target.Theme = theme3;

            root.LayoutManager.ExecuteLayoutPass();

            Assert.Same(target.Theme, theme3);
        }

        [Fact]
        public void TemplatedParent_Theme_Change_Applies_To_Children()
        {
            var theme = CreateDerivedTheme();
            var target = CreateTarget();

            Assert.Null(target.Theme);
            Assert.Null(target.Template);

            var root = CreateRoot(target, theme.BasedOn);

            Assert.NotNull(target.Theme);
            Assert.NotNull(target.Template);

            root.Styles.Add(new Style(x => x.OfType<ThemedControl>().Class("foo"))
            {
                Setters = { new Setter(StyledElement.ThemeProperty, theme) }
            });

            root.LayoutManager.ExecuteLayoutPass();

            var border = Assert.IsType<Border>(target.VisualChild);
            Assert.Equal(Brushes.Red, border.Background);

            target.Classes.Add("foo");
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(Brushes.Green, border.Background);
        }

        [Fact]
        public void TemplatedParent_Theme_Change_Applies_Recursively_To_VisualChildren()
        {
            var theme = CreateDerivedTheme();
            var target = CreateTarget();

            Assert.Null(target.Theme);
            Assert.Null(target.Template);

            var root = CreateRoot(target, theme.BasedOn);

            Assert.NotNull(target.Theme);
            Assert.NotNull(target.Template);

            root.Styles.Add(new Style(x => x.OfType<ThemedControl>().Class("foo"))
            {
                Setters = { new Setter(StyledElement.ThemeProperty, theme) }
            });

            root.LayoutManager.ExecuteLayoutPass();

            var border = Assert.IsType<Border>(target.VisualChild);
            var inner = Assert.IsType<Border>(border.Child);

            Assert.Equal(Brushes.Red, border.Background);
            Assert.Equal(Brushes.Red, inner.Background);

            Assert.Equal(null, inner.BorderBrush);
            Assert.Equal(null, inner.BorderBrush);

            target.Classes.Add("foo");
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(Brushes.Green, border.Background);
            Assert.Equal(Brushes.Green, inner.Background);

            Assert.Equal(Brushes.Cyan, inner.BorderBrush);
            Assert.Equal(Brushes.Cyan, inner.BorderBrush);
        }

        private static ThemedControl CreateTarget()
        {
            return new ThemedControl();
        }

        private static TestRoot CreateRoot(Control child, ControlTheme? theme = null)
        {
            var result = new TestRoot()
            {
                Styles =
                {
                    new Style(x => x.OfType<ThemedControl>())
                    {
                        Setters = { new Setter(StyledElement.ThemeProperty, theme ?? CreateTheme()) }
                    }
                }
            };

            result.Child = child;
            result.LayoutManager.ExecuteInitialLayoutPass();
            return result;
        }
    }

    private static ControlTheme CreateTheme(string tag = "theme")
    {
        var template = new FuncControlTemplate<ThemedControl>(
            (o, n) => new Border() { Child = new Border() });

        return new ControlTheme
        {
            TargetType = typeof(ThemedControl),
            Setters =
            {
                new Setter(Control.TagProperty, tag),
                new Setter(TemplatedControl.TemplateProperty, template),
                new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(5)),
            },
            Children =
            {
                new Style(x => x.Nesting().Template().OfType<Border>())
                {
                    Setters =
                    {
                        new Setter(Border.BackgroundProperty, Brushes.Red),
                        new Setter(Control.TagProperty, tag),
                    }
                },
                new Style(x => x.Nesting().Class("foo").Template().OfType<Border>())
                {
                    Setters = { new Setter(Border.BackgroundProperty, Brushes.Green) }
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
                    Setters = { new Setter(Border.BorderBrushProperty, Brushes.Yellow) }
                },
                new Style(x => x.Nesting().Class("foo").Template().OfType<Border>())
                {
                    Setters = { new Setter(Border.BorderBrushProperty, Brushes.Cyan) }
                },
            }
        };
    }

    private class ThemedControl : Controls.Primitives.TemplatedControl
    {
        public Visual? VisualChild => VisualChildren?.SingleOrDefault();
    }

    private class ThemedControl2 : Controls.Primitives.TemplatedControl
    {
        public Visual? VisualChild => VisualChildren?.SingleOrDefault();
    }

    private class DerivedThemedControl : ThemedControl
    {
    }
}

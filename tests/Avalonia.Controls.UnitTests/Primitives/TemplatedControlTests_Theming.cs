using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class TemplatedControlTests_Theming
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
            
            Assert.Equal(border.Background, Brushes.Red);

            target.Classes.Add("foo");
            Assert.Equal(border.Background, Brushes.Green);
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
            Assert.Equal(border.Background, Brushes.Red);
        }

        private static ThemedControl CreateTarget()
        {
            return new ThemedControl
            {
                Theme = CreateTheme(),
            };
        }

        private static ControlTheme CreateTheme()
        {
            var template = new FuncControlTemplate<ThemedControl>((o, n) =>
                new Border { Name = "PART_Border" });

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

        private static TestRoot CreateRoot(IControl child)
        {
            var result = new TestRoot(child);
            result.LayoutManager.ExecuteInitialLayoutPass();
            return result;
        }

        private class ThemedControl : TemplatedControl
        {
            public IVisual? VisualChild => VisualChildren?.SingleOrDefault();
        }
    }
}

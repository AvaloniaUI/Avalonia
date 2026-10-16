using System;
using Avalonia.Base.UnitTests.Layout;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class ContainerTests
    {
        [Fact]
        public void Container_Cannot_Be_Added_To_Style_Children()
        {
            var target = new ContainerQuery();
            var style = new Style();

            Assert.Throws<InvalidOperationException>(() => style.Children.Add(target));
        }

        [Fact]
        public void Container_Width_Queries_Matches()
        {
            using var app = UnitTestApplication.Start();
            var root = new LayoutTestRoot()
            {
                ClientSize = new Size(400, 400)
            };
            var containerQuery1 = new ContainerQuery(x => new WidthQuery(x, StyleQueryComparisonOperator.LessThanOrEquals, 500));
            containerQuery1.Children.Add(new Style(x => x.Is<Border>())
            {
                Setters = { new Setter(Control.WidthProperty, 200.0) }
            });
            var containerQuery2 = new ContainerQuery(x => new WidthQuery(x, StyleQueryComparisonOperator.GreaterThan, 500));
            containerQuery2.Children.Add(new Style(x => x.Is<Border>())
            {
                Setters = { new Setter(Control.WidthProperty, 500.0) }
            });
            root.Styles.Add(containerQuery1);
            root.Styles.Add(containerQuery2);
            var child = new Border()
            {
                Name = "Child",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };
            var stack = new StackPanel();
            stack.Children.Add(child);
            var border = new Border()
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                Child = stack,
                Name = "Parent"
            };
            Container.SetSizing(border, Avalonia.Styling.ContainerSizing.Width);

            root.Child = border;

            root.LayoutManager.ExecuteInitialLayoutPass();
            Assert.Equal(200, child.Width);

            root.ClientSize = new Size(600, 600);
            root.InvalidateMeasure();

            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(500, child.Width);
        }

        [Fact]
        public void Container_Height_Queries_Matches()
        {
            using var app = UnitTestApplication.Start();
            var root = new LayoutTestRoot()
            {
                ClientSize = new Size(400, 400)
            };
            var containerQuery1 = new ContainerQuery(x => new HeightQuery(x, StyleQueryComparisonOperator.LessThanOrEquals, 500));
            containerQuery1.Children.Add(new Style(x => x.Is<Border>())
            {
                Setters = { new Setter(Control.HeightProperty, 200.0) }
            });
            var containerQuery2 = new ContainerQuery(x => new HeightQuery(x, StyleQueryComparisonOperator.GreaterThan, 500));
            containerQuery2.Children.Add(new Style(x => x.Is<Border>())
            {
                Setters = { new Setter(Control.HeightProperty, 500.0) }
            });
            root.Styles.Add(containerQuery1);
            root.Styles.Add(containerQuery2);
            var child = new Border()
            {
                Name = "Child",
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
            };
            var stack = new StackPanel();
            stack.Children.Add(child);
            var border = new Border()
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                Child = stack,
                Name = "Parent"
            };
            Container.SetSizing(border, Avalonia.Styling.ContainerSizing.Height);

            root.Child = border;

            root.LayoutManager.ExecuteInitialLayoutPass();
            Assert.Equal(200, child.Height);

            root.ClientSize = new Size(600, 600);
            root.InvalidateMeasure();

            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(500, child.Height);
        }

        [Fact]
        public void Container_Width_Queries_Matches_Name()
        {
            using var app = UnitTestApplication.Start();
            var root = new LayoutTestRoot()
            {
                ClientSize = new Size(600, 600)
            };
            var containerQuery1 = new ContainerQuery(x => new WidthQuery(x, StyleQueryComparisonOperator.LessThanOrEquals, 500));
            containerQuery1.Children.Add(new Style(x => x.Is<Border>())
            {
                Setters = { new Setter(Control.WidthProperty, 200.0) }
            });
            var containerQuery2 = new ContainerQuery(x => new WidthQuery(x, StyleQueryComparisonOperator.LessThanOrEquals, 500), "TEST");
            containerQuery2.Children.Add(new Style(x => x.Is<Border>())
            {
                Setters = { new Setter(Control.WidthProperty, 300.0) }
            });
            root.Styles.Add(containerQuery1);
            root.Styles.Add(containerQuery2);
            var child = new Border()
            {
                Name = "Child",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
            };
            var controlInner = new ContentControl()
            {
                Width = 400,
                Height = 400,
                Content = child,
                Name = "Inner"
            };
            Container.SetSizing(controlInner, Avalonia.Styling.ContainerSizing.Width);
            Container.SetName(controlInner, "TEST");
            var stack = new StackPanel();
            stack.Children.Add(controlInner);
            var border = new Border()
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                Child = stack,
                Name = "Parent"
            };
            Container.SetSizing(border, Avalonia.Styling.ContainerSizing.Width);

            root.Child = border;

            root.LayoutManager.ExecuteInitialLayoutPass();

            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(300, child.Width);
        }

        [Fact]
        public void Container_Height_Queries_Matches_Name()
        {
            using var app = UnitTestApplication.Start();
            var root = new LayoutTestRoot()
            {
                ClientSize = new Size(600, 600)
            };
            var containerQuery1 = new ContainerQuery(x => new HeightQuery(x, StyleQueryComparisonOperator.LessThanOrEquals, 500));
            containerQuery1.Children.Add(new Style(x => x.Is<Border>())
            {
                Setters = { new Setter(Control.HeightProperty, 200.0) }
            });
            var containerQuery2 = new ContainerQuery(x => new HeightQuery(x, StyleQueryComparisonOperator.LessThanOrEquals, 450), "TEST");
            containerQuery2.Children.Add(new Style(x => x.Is<Border>())
            {
                Setters = { new Setter(Control.HeightProperty, 300.0) }
            });
            root.Styles.Add(containerQuery1);
            root.Styles.Add(containerQuery2);
            var child = new Border()
            {
                Name = "Child",
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch
            };
            var controlInner = new ContentControl()
            {
                Width = 400,
                Height = 400,
                Content = child,
                Name = "Inner"
            };
            Container.SetSizing(controlInner, Avalonia.Styling.ContainerSizing.Height);
            Container.SetName(controlInner, "TEST");
            var stack = new StackPanel();
            stack.Children.Add(controlInner);
            var border = new Border()
            {
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
                Child = stack,
                Name = "Parent"
            };
            Container.SetSizing(border, Avalonia.Styling.ContainerSizing.Height);

            root.Child = border;

            root.LayoutManager.ExecuteInitialLayoutPass();

            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(300, child.Height);
        }

        [Fact]
        public void Container_Does_Not_Query_Itself()
        {
            using var app = UnitTestApplication.Start();
            var root = new LayoutTestRoot
            {
                ClientSize = new Size(400, 400)
            };
            var containerQuery = new ContainerQuery(x => new WidthQuery(x, StyleQueryComparisonOperator.LessThanOrEquals, 500));
            containerQuery.Children.Add(new Style(x => x.Is<Border>())
            {
                Setters = { new Setter(Visual.OpacityProperty, 0.5) }
            });
            root.Styles.Add(containerQuery);
            var container = new Border();
            Container.SetSizing(container, ContainerSizing.Width);
            root.Child = container;

            root.LayoutManager.ExecuteInitialLayoutPass();

            Assert.Equal(1, container.Opacity);
        }

        [Fact]
        public void Container_Query_Tracks_Non_Visual_Target_After_Logical_Reparenting()
        {
            using var app = UnitTestApplication.Start(TestServices.TextServices);
            var root = new LayoutTestRoot
            {
                ClientSize = new Size(1000, 400)
            };
            var containerQuery = new ContainerQuery(x => new WidthQuery(x, StyleQueryComparisonOperator.LessThanOrEquals, 500));
            containerQuery.Children.Add(new Style(x => x.Is<Run>())
            {
                Setters = { new Setter(StyledElement.DataContextProperty, "matched") }
            });
            var theme = new ControlTheme(typeof(Run));
            theme.Children.Add(containerQuery);
            var target = new Run
            {
                Theme = theme
            };
            target.ApplyStyling();
            var smallContainer = new TextBlock
            {
                Width = 400
            };
            var largeContainer = new TextBlock
            {
                Width = 600
            };
            Container.SetSizing(smallContainer, ContainerSizing.Width);
            Container.SetSizing(largeContainer, ContainerSizing.Width);
            root.Child = new StackPanel
            {
                Children =
                {
                    smallContainer,
                    largeContainer
                }
            };

            root.LayoutManager.ExecuteInitialLayoutPass();

            smallContainer.Inlines!.Add(target);
            Assert.Equal("matched", target.DataContext);

            smallContainer.Inlines.Remove(target);
            largeContainer.Inlines!.Add(target);
            Assert.Null(target.DataContext);

            largeContainer.Width = 400;
            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal("matched", target.DataContext);
        }

        [Theory]
        [InlineData(ContainerQueryType.Width)]
        [InlineData(ContainerQueryType.Height)]
        [InlineData(ContainerQueryType.And)]
        [InlineData(ContainerQueryType.Or)]
        public void Container_Query_Matches_Non_Visual_StyledElement(ContainerQueryType queryType)
        {
            using var app = UnitTestApplication.Start();
            var root = new LayoutTestRoot
            {
                ClientSize = queryType == ContainerQueryType.Or ? new Size(400, 600) : new Size(400, 400)
            };
            var widthQuery = new WidthQuery(null, StyleQueryComparisonOperator.LessThanOrEquals, 500);
            var heightQuery = new HeightQuery(null, StyleQueryComparisonOperator.LessThanOrEquals, 500);
            var containerQuery = new ContainerQuery
            {
                Query = queryType switch
                {
                    ContainerQueryType.Width => widthQuery,
                    ContainerQueryType.Height => heightQuery,
                    ContainerQueryType.And => StyleQueries.And(widthQuery, heightQuery),
                    ContainerQueryType.Or => StyleQueries.Or(widthQuery, heightQuery),
                    _ => throw new ArgumentOutOfRangeException(nameof(queryType))
                }
            };
            containerQuery.Children.Add(new Style(x => x.Is<NonVisualStyledElement>())
            {
                Setters = { new Setter(NonVisualStyledElement.ValueProperty, 42) }
            });
            root.Styles.Add(containerQuery);
            var container = new Border();
            Container.SetSizing(container, queryType switch
            {
                ContainerQueryType.Width => ContainerSizing.Width,
                ContainerQueryType.Height => ContainerSizing.Height,
                _ => ContainerSizing.WidthAndHeight
            });
            root.Child = container;
            var target = new NonVisualStyledElement();
            ((ISetLogicalParent)target).SetParent(container);

            root.LayoutManager.ExecuteInitialLayoutPass();
            Assert.Equal(42, target.Value);

            root.ClientSize = queryType switch
            {
                ContainerQueryType.Width or ContainerQueryType.And => new Size(600, 400),
                ContainerQueryType.Height => new Size(400, 600),
                ContainerQueryType.Or => new Size(600, 600),
                _ => throw new ArgumentOutOfRangeException(nameof(queryType))
            };
            root.InvalidateMeasure();
            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(0, target.Value);
        }

        [Fact]
        public void Container_Query_Uses_Named_Visual_Ancestor_For_Non_Visual_Target()
        {
            using var app = UnitTestApplication.Start();
            var root = new LayoutTestRoot
            {
                ClientSize = new Size(1000, 400)
            };
            var containerQuery = new ContainerQuery(x => new WidthQuery(x, StyleQueryComparisonOperator.LessThanOrEquals, 500), "target");
            containerQuery.Children.Add(new Style(x => x.Is<NonVisualStyledElement>())
            {
                Setters = { new Setter(NonVisualStyledElement.ValueProperty, 42) }
            });
            root.Styles.Add(containerQuery);
            var innerContainer = new Border
            {
                Width = 400
            };
            Container.SetName(innerContainer, "other");
            Container.SetSizing(innerContainer, ContainerSizing.Width);
            var namedContainer = new Border
            {
                Width = 600,
                Child = innerContainer
            };
            Container.SetName(namedContainer, "target");
            Container.SetSizing(namedContainer, ContainerSizing.Width);
            root.Child = namedContainer;
            var target = new NonVisualStyledElement();
            ((ISetLogicalParent)target).SetParent(innerContainer);

            root.LayoutManager.ExecuteInitialLayoutPass();
            Assert.Equal(0, target.Value);

            namedContainer.Width = 400;
            root.LayoutManager.ExecuteLayoutPass();
            Assert.Equal(42, target.Value);
        }

        private sealed class NonVisualStyledElement : StyledElement
        {
            public static readonly StyledProperty<int> ValueProperty =
                AvaloniaProperty.Register<NonVisualStyledElement, int>(nameof(Value));

            public int Value
            {
                get => GetValue(ValueProperty);
                set => SetValue(ValueProperty, value);
            }
        }

        public enum ContainerQueryType
        {
            Width,
            Height,
            And,
            Or
        }
    }
}

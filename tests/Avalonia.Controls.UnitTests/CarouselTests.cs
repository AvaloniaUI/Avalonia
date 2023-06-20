using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class CarouselTests
    {
        [Fact]
        public void First_Item_Should_Be_Selected_By_Default()
        {
            using var app = Start();
            var target = new Carousel
            {
                Template = CarouselTemplate(),
                ItemsSource = new[]
                {
                    "Foo",
                    "Bar"
                }
            };

            Prepare(target);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("Foo", target.SelectedItem);
        }

        [Fact]
        public void LogicalChild_Should_Be_Selected_Item()
        {
            using var app = Start();
            var target = new Carousel
            {
                Template = CarouselTemplate(),
                ItemsSource = new[]
                {
                    "Foo",
                    "Bar"
                }
            };

            Prepare(target);

            Assert.Single(target.GetRealizedContainers());

            var child = GetContainerTextBlock(target.GetRealizedContainers().Single());

            Assert.Equal("Foo", child.Text);
        }

        [Fact]
        public void Selected_Item_Changes_To_First_Item_When_Items_Property_Changes()
        {
            using var app = Start();
            var items = new ObservableCollection<string>
            {
                "Foo",
                "Bar",
                "FooBar"
            };

            var target = new Carousel
            {
                Template = CarouselTemplate(),
                ItemsSource = items,
            };

            Prepare(target);

            Assert.Single(target.GetRealizedContainers());

            var child = GetContainerTextBlock(target.GetRealizedContainers().Single());

            Assert.Equal("Foo", child.Text);

            var newItems = items.ToList();
            newItems.RemoveAt(0);
            Layout(target);

            target.ItemsSource = newItems;
            Layout(target);

            child = GetContainerTextBlock(target.GetRealizedContainers().Single());

            Assert.Equal("Bar", child.Text);
        }

        [Fact]
        public void Selected_Item_Changes_To_First_Item_When_Item_Added()
        {
            using var app = Start();
            var items = new ObservableCollection<string>();
            var target = new Carousel
            {
                Template = CarouselTemplate(),
                ItemsSource = items,
            };

            Prepare(target);

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Empty(target.GetRealizedContainers());

            items.Add("Foo");
            Layout(target);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Single(target.GetRealizedContainers());
        }

        [Fact]
        public void Selected_Index_Changes_To_None_When_Items_Assigned_Null()
        {
            using var app = Start();
            var items = new ObservableCollection<string>
            {
                "Foo",
                "Bar",
                "FooBar"
            };

            var target = new Carousel
            {
                Template = CarouselTemplate(),
                ItemsSource = items,
            };

            Prepare(target);

            Assert.Equal(1, target.GetRealizedContainers().Count());

            var child = GetContainerTextBlock(target.GetRealizedContainers().First());

            Assert.Equal("Foo", child.Text);

            target.ItemsSource = null;
            Layout(target);

            var numChildren = target.GetRealizedContainers().Count();

            Assert.Equal(-1, target.SelectedIndex);
        }

        [Fact]
        public void Selected_Index_Is_Maintained_Carousel_Created_With_Non_Zero_SelectedIndex()
        {
            using var app = Start();
            var items = new ObservableCollection<string>
            {
                "Foo",
                "Bar",
                "FooBar"
            };

            var target = new Carousel
            {
                Template = CarouselTemplate(),
                ItemsSource = items,
                SelectedIndex = 2
            };

            Prepare(target);

            Assert.Equal("FooBar", target.SelectedItem);

            var child = GetContainerTextBlock(target.GetRealizedContainers().LastOrDefault());

            Assert.Equal("FooBar", child.Text);
        }

        [Fact]
        public void Selected_Item_Changes_To_Next_First_Item_When_Item_Removed_From_Beggining_Of_List()
        {
            using var app = Start();
            var items = new ObservableCollection<string>
            {
                "Foo",
                "Bar",
                "FooBar"
            };

            var target = new Carousel
            {
                Template = CarouselTemplate(),
                ItemsSource = items,
            };

            Prepare(target);

            var child = GetContainerTextBlock(target.GetRealizedContainers().First());

            Assert.Equal("Foo", child.Text);

            items.RemoveAt(0);
            Layout(target);

            child = GetContainerTextBlock(target.GetRealizedContainers().First());

            Assert.IsType<TextBlock>(child);
            Assert.Equal("Bar", child.Text);
        }

        [Fact]
        public void Selected_Item_Changes_To_First_Item_If_SelectedItem_Is_Removed_From_Middle()
        {
            using var app = Start();
            var items = new ObservableCollection<string>
            {
                "Foo",
                "Bar",
                "FooBar"
            };

            var target = new Carousel
            {
                Template = CarouselTemplate(),
                ItemsSource = items,
            };

            Prepare(target);

            target.SelectedIndex = 1;

            items.RemoveAt(1);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("Foo", target.SelectedItem);
        }

        [Fact]
        public void SelectedItem_Validation()
        {
            using (UnitTestApplication.Start(TestServices.MockThreadingInterface))
            {
                var target = new Carousel
                {
                    Template = CarouselTemplate(),
                };

                Prepare(target);

                var exception = new System.InvalidCastException("failed validation");
                var textObservable =
                    new BehaviorSubject<BindingNotification>(new BindingNotification(exception,
                        BindingErrorType.DataValidationError));
                target.Bind(ComboBox.SelectedItemProperty, textObservable);

                Assert.True(DataValidationErrors.GetHasErrors(target));
                Assert.True(DataValidationErrors.GetErrors(target).SequenceEqual(new[] { exception }));
            }
        }

        [Fact]
        public void Can_Move_Forward_Back_Forward()
        {
            using var app = Start();
            var items = new[] { "foo", "bar" };
            var target = new Carousel
            {
                Template = CarouselTemplate(),
                ItemsSource = items,
            };

            Prepare(target);

            target.SelectedIndex = 1;
            Layout(target);

            Assert.Equal(1, target.SelectedIndex);

            target.SelectedIndex = 0;
            Layout(target);

            Assert.Equal(0, target.SelectedIndex);

            target.SelectedIndex = 1;
            Layout(target);

            Assert.Equal(1, target.SelectedIndex);
        }

        [Fact]
        public void Can_Move_Forward_Back_Forward_With_Control_Items()
        {
            // Issue #11119
            using var app = Start();
            var items = new[] { new Canvas(), new Canvas() };
            var target = new Carousel
            {
                Template = CarouselTemplate(),
                ItemsSource = items,
            };

            Prepare(target);

            target.SelectedIndex = 1;
            Layout(target);

            Assert.Equal(1, target.SelectedIndex);

            target.SelectedIndex = 0;
            Layout(target);

            Assert.Equal(0, target.SelectedIndex);

            target.SelectedIndex = 1;
            target.PropertyChanged += (s, e) =>
            {
                if (e.Property == Carousel.SelectedIndexProperty)
                {
                }
            };
            Layout(target);

            Assert.Equal(1, target.SelectedIndex);
        }

        private static IDisposable Start() => UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

        private static void Prepare(Carousel target)
        {
            var root = new TestRoot(target);
            root.LayoutManager.ExecuteInitialLayoutPass();
        }

        private static void Layout(Carousel target)
        {
            ((ILayoutRoot)target.GetVisualRoot()).LayoutManager.ExecuteLayoutPass();
        }

        private static IControlTemplate CarouselTemplate()
        {
            return new FuncControlTemplate((c, ns) =>
                new ScrollViewer
                {
                    Name = "PART_ScrollViewer",
                    Template = ScrollViewerTemplate(),
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    Content = new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [~ItemsPresenter.ItemsPanelProperty] = c[~ItemsControl.ItemsPanelProperty],
                    }.RegisterInNameScope(ns)
                }.RegisterInNameScope(ns));
        }

        private static FuncControlTemplate ScrollViewerTemplate()
        {
            return new FuncControlTemplate<ScrollViewer>((parent, scope) =>
                new Panel
                {
                    Children =
                    {
                        new ScrollContentPresenter
                        {
                            Name = "PART_ContentPresenter",
                        }.RegisterInNameScope(scope),
                    }
                });
        }

        private static TextBlock GetContainerTextBlock(object control)
        {
            var contentPresenter = Assert.IsType<ContentPresenter>(control);
            return Assert.IsType<TextBlock>(contentPresenter.Child);
        }
    }
}

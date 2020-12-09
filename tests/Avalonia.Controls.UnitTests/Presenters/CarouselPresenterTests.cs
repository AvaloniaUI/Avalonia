using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.UnitTests;
using Moq;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests.Presenters
{
    public class CarouselPresenterTests
    {
        [Fact]
        public void Should_Register_With_Host_When_TemplatedParent_Set()
        {
            var host = new Mock<IItemsPresenterHost>();
            var target = new CarouselPresenter();

            target.SetValue(Control.TemplatedParentProperty, host.Object);

            host.Verify(x => x.RegisterItemsPresenter(target));
        }

        [Fact]
        public void ItemTemplate_Should_Be_Picked_Up_From_TemplatedControl()
        {
            var parent = new Carousel();
            var target = new CarouselPresenter
            {
                [StyledElement.TemplatedParentProperty] = parent,
            };

            Assert.NotNull(parent.ItemContainerGenerator);
            Assert.Same(parent.ItemContainerGenerator, target.ElementFactory);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Initially_Materialize_Selected_Container(bool isVirtualized)
        {
            using var app = Start();

            var (target, _) = CreateTarget(isVirtualized);

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Changing_SelectedIndex_Should_Show_Page(bool isVirtualized)
        {
            using var app = Start();

            var (target, root) = CreateTarget(isVirtualized);

            AssertState(target);

            target.SelectedIndex = 1;

            Assert.False(target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Inserting_Item_At_SelectedIndex(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items, selectedIndex: 1);

            items.Insert(1, "baz");

            Assert.Equal(2, target.SelectedIndex);
            Assert.Equal(isVirtualized, target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Inserting_Item_Before_SelectedIndex(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items, selectedIndex: 1);

            items.Insert(0, "baz");

            Assert.Equal(2, target.SelectedIndex);
            Assert.Equal(isVirtualized, target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Inserting_Item_After_SelectedIndex(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items);

            items.Insert(1, "baz");

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(isVirtualized, target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Removing_Selected_Item(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items, selectedIndex: 0);

            items.RemoveAt(0);

            Assert.False(target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(0, target.SelectedIndex);
            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Removing_Selected_Item_At_End(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items, selectedIndex: 1);

            items.RemoveAt(1);

            Assert.False(target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(0, target.SelectedIndex);
            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Removing_Item_After_SelectedIndex(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items, selectedIndex: 0);

            items.RemoveAt(1);

            root.LayoutManager.ExecuteLayoutPass();

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Replacing_Selected_Item(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items, selectedIndex: 1);

            items[1] = "baz";

            Assert.False(target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(0, target.SelectedIndex);
            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Replacing_Non_SelectedItem(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items, selectedIndex: 1);

            items[0] = "baz";

            Assert.Equal(1, target.SelectedIndex);
            Assert.Equal(isVirtualized, target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Should_Handle_Moving_SelectedItem(bool isVirtualized)
        {
            using var app = Start();

            var items = new ObservableCollection<string> { "foo", "bar" };
            var (target, root) = CreateTarget(isVirtualized, items: items, selectedIndex: 1);

            items.Move(1, 0);

            Assert.False(target.IsMeasureValid);
            root.LayoutManager.ExecuteLayoutPass();

            Assert.Equal(1, target.SelectedIndex);
            AssertState(target);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Switching_IsVirtualized_Should_Reset_Containers(bool isVirtualized)
        {
            using var app = Start();
            var (target, root) = CreateTarget(isVirtualized);

            target.IsVirtualized = !target.IsVirtualized;

            Assert.False(target.IsMeasureValid);
            Assert.Empty(target.Children);
            root.LayoutManager.ExecuteLayoutPass();

            AssertState(target);
        }

        private static (CarouselPresenter, TestRoot) CreateTarget(
            bool isVirtualized,
            IEnumerable? items = null,
            int? selectedIndex = null)
        {
            RuntimeHelpers.RunClassConstructor(typeof(CarouselPresenter).TypeHandle);

            var carousel = new Carousel
            {
                Items = items ?? new[] { "foo", "bar" },
                SelectedIndex = selectedIndex ?? 0,
                IsVirtualized = isVirtualized,
                Template = new FuncControlTemplate<Carousel>((c, ns) =>
                    new CarouselPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [!ItemsControl.ItemsProperty] = c[!ItemsControl.ItemsViewProperty],
                        [!CarouselPresenter.IsVirtualizedProperty] = c[!Carousel.IsVirtualizedProperty],
                        [!CarouselPresenter.SelectedIndexProperty] = c[!Carousel.SelectedIndexProperty],
                    }),
            };

            var root = new TestRoot(carousel);
            root.LayoutManager.ExecuteInitialLayoutPass();

            return ((CarouselPresenter)carousel.Presenter!, root);
        }

        private static void AssertState(CarouselPresenter target)
        {
            if (target.IsVirtualized)
                AssertVirtualizedState(target);
            else
                AssertNonVirtualizedState(target);
        }

        private static void AssertVirtualizedState(CarouselPresenter target)
        {
            var items = (ItemsSourceView)target.Items!;
            var index = target.SelectedIndex;
            var content = items[index];
            var child = Assert.Single(target.Children);
            var presenter = Assert.IsType<ContentPresenter>(child);
            var visible = Assert.Single(target.RealizedElements.Where(x => x.IsVisible));

            Assert.Same(child, visible);
            Assert.Equal(content, presenter.Content);
            Assert.Single(target.RealizedElements);
        }

        private static void AssertNonVirtualizedState(CarouselPresenter target)
        {
            var items = (ItemsSourceView)target.Items!;

            Assert.True(target.Children.Count <= items.Count);
            Assert.True(target.RealizedElements.Count() <= items.Count);

            for (var i = 0; i < items?.Count; ++i)
            {
                var content = items[i];
                var container = target.TryGetElement(i);
                var presenter = Assert.IsType<ContentPresenter>(container);

                Assert.Equal(i == target.SelectedIndex, presenter.IsVisible);
                Assert.Equal(content, presenter.Content);
                Assert.Equal(i, target.GetElementIndex(presenter));
            }
        }

        private static IDisposable Start()
        {
            var services = TestServices.MockPlatformRenderInterface;
            return UnitTestApplication.Start(services);
        }
    }
}

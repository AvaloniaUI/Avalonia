using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests.Presenters
{
    public class ItemsPresenterTests
    {
        [Fact]
        public void Should_Register_With_Host_When_TemplatedParent_Set()
        {
            var host = new ItemsControl();
            var target = new ItemsPresenter();

            Assert.Null(host.Presenter);

            target.TemplatedParent = host;

            Assert.Same(target, host.Presenter);
        }

        [Fact]
        public void Panel_Should_Be_Visual_Child()
        {
            var (target, _, _) = CreateTarget();
            var child = target.GetVisualChildren().Single();

            Assert.Equal(target.Panel, child);
        }

        public class NonVirtualizingPanel
        {
            [Fact]
            public void Creates_Containers_For_Initial_Items()
            {
                using var app = Start();
                var items = new[] { "foo", "bar", "baz" };
                var (target, _, _) = CreateTarget(items);
                var panel = Assert.IsType<StackPanel>(target.Panel);

                AssertContainers(panel, items);
            }

            [Fact]
            public void Creates_Containers_For_Inserted_Items()
            {
                using var app = Start();
                var items = new ObservableCollection<string>(new[] { "foo", "bar", "baz" });
                var (target, _, root) = CreateTarget(items);
                var panel = Assert.IsType<StackPanel>(target.Panel);

                items.Insert(1, "foo2");
                root.LayoutManager.ExecuteLayoutPass();

                AssertContainers(panel, items);
            }

            [Fact]
            public void Removes_Containers_For_Removed_Items()
            {
                using var app = Start();
                var items = new ObservableCollection<string>(new[] { "foo", "bar", "baz" });
                var (target, _, root) = CreateTarget(items);
                var panel = Assert.IsType<StackPanel>(target.Panel);

                items.RemoveAt(1);
                root.LayoutManager.ExecuteLayoutPass();

                AssertContainers(panel, items);
            }

            [Fact]
            public void Updates_Containers_For_Moved_Items()
            {
                using var app = Start();
                var items = new ObservableCollection<string>(new[] { "foo", "bar", "baz" });
                var (target, _, root) = CreateTarget(items);
                var panel = Assert.IsType<StackPanel>(target.Panel);

                items.Move(0, 2);
                root.LayoutManager.ExecuteLayoutPass();

                AssertContainers(panel, items);
            }

            [Fact]
            public void Updates_Containers_For_Replaced_Items()
            {
                using var app = Start();
                var items = new ObservableCollection<string>(new[] { "foo", "bar", "baz" });
                var (target, _, root) = CreateTarget(items);
                var panel = Assert.IsType<StackPanel>(target.Panel);

                items[1] = "bar2";
                root.LayoutManager.ExecuteLayoutPass();

                AssertContainers(panel, items);
            }

            [Fact]
            public void Updates_Containers_On_Items_Changed()
            {
                using var app = Start();
                var items = new ObservableCollection<string>(new[] { "foo", "bar", "baz" });
                var (target, itemsControl, root) = CreateTarget(items);
                var panel = Assert.IsType<StackPanel>(target.Panel);

                var newItems = new[] { "qux", "quux", "corge" };
                itemsControl.ItemsSource = newItems;
                root.LayoutManager.ExecuteLayoutPass();

                AssertContainers(panel, newItems);
            }

            private static void AssertContainers(StackPanel panel, IReadOnlyList<string> items)
            {
                Assert.Equal(items.Count, panel.Children.Count);

                for (var i = 0; i < items.Count; i++)
                {
                    var container = Assert.IsType<ContentPresenter>(panel.Children[i]);
                    Assert.Equal(items[i], container.DataContext);
                    Assert.Equal(items[i], container.Content);
                }
            }
        }

        private static (ItemsPresenter, ItemsControl, TestRoot) CreateTarget(IReadOnlyList<string>? items = null)
        {
            var result = new ItemsPresenter();

            var itemsControl = new ItemsControl
            {
                ItemsSource = items,
                Template = new FuncControlTemplate<ItemsControl>((_, _) => result)
            };

            var root = new TestRoot(itemsControl);
            root.LayoutManager.ExecuteInitialLayoutPass();
            return (result, itemsControl, root);
        }

        private static IDisposable Start() => UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
    }
}

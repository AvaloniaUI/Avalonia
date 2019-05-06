// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Moq;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Xunit;
using System.Collections.ObjectModel;
using System.Collections;

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
        public void ApplyTemplate_Should_Create_Panel()
        {
            var target = new CarouselPresenter
            {
                ItemsPanel = new FuncTemplate<IPanel>(() => new Panel()),
            };

            target.ApplyTemplate();

            Assert.IsType<Panel>(target.Panel);
        }

        [Fact]
        public void ItemContainerGenerator_Should_Be_Picked_Up_From_TemplatedControl()
        {
            var parent = new TestItemsControl();
            var target = new CarouselPresenter
            {
                [StyledElement.TemplatedParentProperty] = parent,
            };

            Assert.IsType<ItemContainerGenerator<TestItem>>(target.ItemContainerGenerator);
        }

        public class Virtualized
        {
            [Fact]
            public void Should_Initially_Materialize_Selected_Container()
            {
                var target = new CarouselPresenter
                {
                    Items = new[] { "foo", "bar" },
                    SelectedIndex = 0,
                    IsVirtualized = true,
                };

                target.ApplyTemplate();

                AssertSingle(target);
            }

            [Fact]
            public void Should_Initially_Materialize_Nothing_If_No_Selected_Container()
            {
                var target = new CarouselPresenter
                {
                    Items = new[] { "foo", "bar" },
                    IsVirtualized = true,
                };

                target.ApplyTemplate();

                Assert.Empty(target.Panel.Children);
                Assert.Empty(target.ItemContainerGenerator.Containers);
            }

            [Fact]
            public void Switching_To_Virtualized_Should_Reset_Containers()
            {
                var target = new CarouselPresenter
                {
                    Items = new[] { "foo", "bar" },
                    SelectedIndex = 0,
                    IsVirtualized = false,
                };

                target.ApplyTemplate();
                target.IsVirtualized = true;

                AssertSingle(target);
            }

            [Fact]
            public void Changing_SelectedIndex_Should_Show_Page()
            {
                var target = new CarouselPresenter
                {
                    Items = new[] { "foo", "bar" },
                    SelectedIndex = 0,
                    IsVirtualized = true,
                };

                target.ApplyTemplate();
                AssertSingle(target);

                target.SelectedIndex = 1;
                AssertSingle(target);
            }

            [Fact]
            public void Should_Remove_NonCurrent_Page()
            {
                var target = new CarouselPresenter
                {
                    Items = new[] { "foo", "bar" },
                    IsVirtualized = true,
                    SelectedIndex = 0,
                };

                target.ApplyTemplate();
                AssertSingle(target);

                target.SelectedIndex = 1;
                AssertSingle(target);

            }

            [Fact]
            public void Should_Handle_Inserting_Item_At_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 1,
                    IsVirtualized = true,
                };

                target.ApplyTemplate();

                items.Insert(1, "item1a");
                AssertSingle(target);
            }

            [Fact]
            public void Should_Handle_Inserting_Item_Before_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 2,
                    IsVirtualized = true,
                };

                target.ApplyTemplate();

                items.Insert(1, "item1a");
                AssertSingle(target);
            }

            [Fact]
            public void Should_Do_Nothing_When_Inserting_Item_After_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 1,
                    IsVirtualized = true,
                };

                target.ApplyTemplate();
                var child = AssertSingle(target);
                items.Insert(2, "after");
                Assert.Same(child, AssertSingle(target));
            }

            [Fact]
            public void Should_Handle_Removing_Item_At_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 1,
                    IsVirtualized = true,
                };

                target.ApplyTemplate();

                items.RemoveAt(1);
                AssertSingle(target);
            }

            [Fact]
            public void Should_Handle_Removing_Item_Before_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 1,
                    IsVirtualized = true,
                };

                target.ApplyTemplate();

                items.RemoveAt(0);
                AssertSingle(target);
            }

            [Fact]
            public void Should_Do_Nothing_When_Removing_Item_After_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 1,
                    IsVirtualized = true,
                };

                target.ApplyTemplate();
                var child = AssertSingle(target);
                items.RemoveAt(2);
                Assert.Same(child, AssertSingle(target));
            }

            [Fact]
            public void Should_Handle_Removing_SelectedItem_When_Its_Last()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 2,
                    IsVirtualized = true,
                };

                target.ApplyTemplate();

                items.RemoveAt(2);
                Assert.Equal(1, target.SelectedIndex);
                AssertSingle(target);
            }

            [Fact]
            public void Should_Handle_Removing_Last_Item()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 0,
                    IsVirtualized = true,
                };

                target.ApplyTemplate();

                items.RemoveAt(0);
                Assert.Empty(target.Panel.Children);
                Assert.Empty(target.ItemContainerGenerator.Containers);
            }

            [Fact]
            public void Should_Handle_Replacing_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 1,
                    IsVirtualized = true,
                };

                target.ApplyTemplate();

                items[1] = "replaced";
                AssertSingle(target);
            }

            [Fact]
            public void Should_Do_Nothing_When_Replacing_Non_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 1,
                    IsVirtualized = true,
                };

                target.ApplyTemplate();
                var child = AssertSingle(target);
                items[0] = "replaced";
                Assert.Same(child, AssertSingle(target));
            }

            [Fact]
            public void Should_Handle_Moving_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 1,
                    IsVirtualized = true,
                };

                target.ApplyTemplate();

                items.Move(1, 0);
                AssertSingle(target);
            }

            private static IControl AssertSingle(CarouselPresenter target)
            {
                var items = (IList)target.Items;
                var index = target.SelectedIndex;
                var content = items[index];
                var child = Assert.Single(target.Panel.Children);
                var presenter = Assert.IsType<ContentPresenter>(child);
                var container = Assert.Single(target.ItemContainerGenerator.Containers);
                var visible = Assert.Single(target.Panel.Children.Where(x => x.IsVisible));

                Assert.Same(child, container.ContainerControl);
                Assert.Same(child, visible);
                Assert.Equal(content, presenter.Content);
                Assert.Equal(content, container.Item);
                Assert.Equal(index, container.Index);

                return child;
            }
        }

        public class NonVirtualized
        {
            [Fact]
            public void Should_Initially_Materialize_All_Containers()
            {
                var target = new CarouselPresenter
                {
                    Items = new[] { "foo", "bar" },
                    IsVirtualized = false,
                };

                target.ApplyTemplate();
                AssertAll(target);
            }

            [Fact]
            public void Should_Initially_Show_Selected_Item()
            {
                var target = new CarouselPresenter
                {
                    Items = new[] { "foo", "bar" },
                    SelectedIndex = 1,
                    IsVirtualized = false,
                };

                target.ApplyTemplate();
                AssertAll(target);
            }

            [Fact]
            public void Switching_To_Non_Virtualized_Should_Reset_Containers()
            {
                var target = new CarouselPresenter
                {
                    Items = new[] { "foo", "bar" },
                    SelectedIndex = 0,
                    IsVirtualized = true,
                };

                target.ApplyTemplate();
                target.IsVirtualized = false;

                AssertAll(target);
            }

            [Fact]
            public void Changing_SelectedIndex_Should_Show_Page()
            {
                var target = new CarouselPresenter
                {
                    Items = new[] { "foo", "bar" },
                    SelectedIndex = 0,
                    IsVirtualized = false,
                };

                target.ApplyTemplate();
                AssertAll(target);

                target.SelectedIndex = 1;
                AssertAll(target);
            }

            [Fact]
            public void Should_Handle_Inserting_Item_At_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 1,
                    IsVirtualized = false,
                };

                target.ApplyTemplate();

                items.Insert(1, "item1a");
                AssertAll(target);
            }

            [Fact]
            public void Should_Handle_Inserting_Item_Before_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 2,
                    IsVirtualized = false,
                };

                target.ApplyTemplate();

                items.Insert(1, "item1a");
                AssertAll(target);
            }

            [Fact]
            public void Should_Do_Handle_Inserting_Item_After_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 1,
                    IsVirtualized = false,
                };

                target.ApplyTemplate();
                items.Insert(2, "after");
                AssertAll(target);
            }

            [Fact]
            public void Should_Handle_Removing_Item_At_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 1,
                    IsVirtualized = false,
                };

                target.ApplyTemplate();

                items.RemoveAt(1);
                AssertAll(target);
            }

            [Fact]
            public void Should_Handle_Removing_Item_Before_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 1,
                    IsVirtualized = false,
                };

                target.ApplyTemplate();

                items.RemoveAt(0);
                AssertAll(target);
            }

            [Fact]
            public void Should_Handle_Removing_Item_After_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 1,
                    IsVirtualized = false,
                };

                target.ApplyTemplate();
                items.RemoveAt(2);
                AssertAll(target);
            }

            [Fact]
            public void Should_Handle_Removing_SelectedItem_When_Its_Last()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 2,
                    IsVirtualized = false,
                };

                target.ApplyTemplate();

                items.RemoveAt(2);
                Assert.Equal(1, target.SelectedIndex);
                AssertAll(target);
            }

            [Fact]
            public void Should_Handle_Removing_Last_Item()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 0,
                    IsVirtualized = false,
                };

                target.ApplyTemplate();

                items.RemoveAt(0);
                Assert.Empty(target.Panel.Children);
                Assert.Empty(target.ItemContainerGenerator.Containers);
            }

            [Fact]
            public void Should_Handle_Replacing_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 1,
                    IsVirtualized = false,
                };

                target.ApplyTemplate();

                items[1] = "replaced";
                AssertAll(target);
            }

            [Fact]
            public void Should_Handle_Moving_SelectedItem()
            {
                var items = new ObservableCollection<string>
                {
                    "item0",
                    "item1",
                    "item2",
                };

                var target = new CarouselPresenter
                {
                    Items = items,
                    SelectedIndex = 1,
                    IsVirtualized = false,
                };

                target.ApplyTemplate();

                items.Move(1, 0);
                AssertAll(target);
            }

            private static void AssertAll(CarouselPresenter target)
            {
                var items = (IList)target.Items;

                Assert.Equal(items?.Count ?? 0, target.Panel.Children.Count);
                Assert.Equal(items?.Count ?? 0, target.ItemContainerGenerator.Containers.Count());

                for (var i = 0; i < items?.Count; ++i)
                {
                    var content = items[i];
                    var child = target.Panel.Children[i];
                    var presenter = Assert.IsType<ContentPresenter>(child);
                    var container = target.ItemContainerGenerator.ContainerFromIndex(i);

                    Assert.Same(child, container);
                    Assert.Equal(i == target.SelectedIndex, child.IsVisible);
                    Assert.Equal(content, presenter.Content);
                    Assert.Equal(i, target.ItemContainerGenerator.IndexFromContainer(container));
                }
            }
        }

        private class TestItem : ContentControl
        {
        }

        private class TestItemsControl : ItemsControl
        {
            protected override IItemContainerGenerator CreateItemContainerGenerator()
            {
                return new ItemContainerGenerator<TestItem>(this, TestItem.ContentProperty, null);
            }
        }
    }
}

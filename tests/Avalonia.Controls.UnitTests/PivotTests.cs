using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class PivotTests
    {
        [Fact]
        public void First_Tab_Should_Be_Selected_By_Default()
        {
            PivotItem selected;
            var target = new Pivot
            {
                Template = PivotTemplate(),
                Items = new[]
                {
                    (selected = new PivotItem
                    {
                        Name = "first",
                        Content = "foo",
                    }),
                    new PivotItem
                    {
                        Name = "second",
                        Content = "bar",
                    },
                }
            };

            target.ApplyTemplate();

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal(selected, target.SelectedItem);
        }

        [Fact]
        public void Logical_Children_Should_Be_PivotItems()
        {
            var items = new[]
            {
                new PivotItem
                {
                    Content = "foo"
                },
                new PivotItem
                {
                    Content = "bar"
                },
            };

            var target = new Pivot
            {
                Template = PivotTemplate(),
                Items = items,
            };

            Assert.Equal(items, target.GetLogicalChildren());
            target.ApplyTemplate();
            Assert.Equal(items, target.GetLogicalChildren());
        }

        [Fact]
        public void Removal_Should_Set_First_Tab()
        {
            var collection = new ObservableCollection<PivotItem>()
            {
                new PivotItem
                {
                    Name = "first",
                    Content = "foo",
                },
                new PivotItem
                {
                    Name = "second",
                    Content = "bar",
                },
                new PivotItem
                {
                    Name = "3rd",
                    Content = "barf",
                },
            };

            var target = new Pivot
            {
                Template = PivotTemplate(),
                Items = collection,
            };

            Prepare(target);
            target.SelectedItem = collection[1];

            Assert.Same(collection[1], target.SelectedItem);

            collection.RemoveAt(1);

            Assert.Same(collection[0], target.SelectedItem);
        }

        [Fact]
        public void Removal_Should_Set_New_Item0_When_Item0_Selected()
        {
            var collection = new ObservableCollection<PivotItem>()
            {
                new PivotItem
                {
                    Name = "first",
                    Content = "foo",
                },
                new PivotItem
                {
                    Name = "second",
                    Content = "bar",
                },
                new PivotItem
                {
                    Name = "3rd",
                    Content = "barf",
                },
            };

            var target = new Pivot
            {
                Template = PivotTemplate(),
                Items = collection,
            };

            Prepare(target);
            target.SelectedItem = collection[0];

            Assert.Same(collection[0], target.SelectedItem);

            collection.RemoveAt(0);

            Assert.Same(collection[0], target.SelectedItem);
        }

        [Fact]
        public void Removal_Should_Set_New_Item0_When_Item0_Selected_With_DataTemplate()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var collection = new ObservableCollection<Item>()
            {
                new Item("first"),
                new Item("second"),
                new Item("3rd"),
            };

            var target = new Pivot
            {
                Template = PivotTemplate(),
                Items = collection,
            };

            Prepare(target);
            target.SelectedItem = collection[0];

            Assert.Same(collection[0], target.SelectedItem);

            collection.RemoveAt(0);

            Assert.Same(collection[0], target.SelectedItem);
        }

        [Fact]
        public void PivotItem_Templates_Should_Be_Set_Before_PivotItem_ApplyTemplate()
        {
            var collection = new[]
            {
                new PivotItem
                {
                    Name = "first",
                    Content = "foo",
                },
                new PivotItem
                {
                    Name = "second",
                    Content = "bar",
                },
                new PivotItem
                {
                    Name = "3rd",
                    Content = "barf",
                },
            };

            var template = new FuncControlTemplate<PivotItem>((x, __) => new Decorator());
            var root = new TestRoot
            {
                Styles =
                {
                    new Style(x => x.OfType<PivotItem>())
                    {
                        Setters =
                        {
                            new Setter(TemplatedControl.TemplateProperty, template)
                        }
                    }
                },
                Child = new Pivot
                {
                    Template = PivotTemplate(),
                    Items = collection,
                }
            };

            Assert.Same(collection[0].Template, template);
            Assert.Same(collection[1].Template, template);
            Assert.Same(collection[2].Template, template);
        }

        /// <summary>
        /// Non-headered control items should result in PivotItems with empty header.
        /// </summary>
        /// <remarks>
        /// If a Pivot is created with non IHeadered controls as its items, don't try to
        /// display the control in the header: if the control is part of the header then
        /// *that* control would also end up in the content region, resulting in dual-parentage 
        /// breakage.
        /// </remarks>
        [Fact]
        public void Non_IHeadered_Control_Items_Should_Be_Ignored()
        {
            var items = new[]
            {
                new TextBlock { Text = "foo" },
                new TextBlock { Text = "bar" },
            };

            var target = new Pivot
            {
                Template = PivotTemplate(),
                Items = items,
            };

            ApplyTemplate(target);

            var logicalChildren = target.ItemsPresenterPart.Panel.GetLogicalChildren();

            var result = logicalChildren
                .OfType<PivotItem>()
                .Select(x => x.Header)
                .ToList();

            Assert.Equal(new object[] { null, null }, result);
        }

        [Fact]
        public void Should_Handle_Changing_To_PivotItem_With_Null_Content()
        {
            Pivot target = new Pivot
            {
                Template = PivotTemplate(),
                Items = new[]
                {
                    new PivotItem { Header = "Foo" },
                    new PivotItem { Header = "Foo", Content = new Decorator() },
                    new PivotItem { Header = "Baz" },
                },
            };

            ApplyTemplate(target);

            target.SelectedIndex = 2;

            var page = (PivotItem)target.SelectedItem;

            Assert.Null(page.Content);
        }

        [Fact]
        public void Should_Not_Propagate_DataContext_To_PivotItem_Content()
        {
            var dataContext = "DataContext";

            var PivotItem = new PivotItem();

            var target = new Pivot
            {
                Template = PivotTemplate(),
                DataContext = dataContext,
                Items = new AvaloniaList<object> { PivotItem }
            };

            ApplyTemplate(target);

            Assert.NotEqual(dataContext, PivotItem.Content);
        }

        [Fact]
        public void Can_Have_Empty_Tab_Control()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'>
    <Pivot Name='tabs' Items='{Binding Tabs}'/>
</Window>";
                var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
                var Pivot = window.FindControl<Pivot>("tabs");

                Pivot.DataContext = new { Tabs = new List<string>() };
                window.ApplyTemplate();

                Assert.Equal(0, Pivot.Items.Count());
            }
        }

        private static IControlTemplate PivotTemplate()
        {
            return new FuncControlTemplate<Pivot>((parent, scope) =>
                new StackPanel
                {
                    Children =
                    {
                        new ItemsPresenter
                        {
                            Name = "PART_ItemsPresenter",
                        }.RegisterInNameScope(scope),
                        new ContentPresenter
                        {
                            Name = "PART_SelectedContentHost",
                            [!ContentPresenter.ContentProperty] = parent[!Pivot.SelectedItemProperty],
                            [!ContentPresenter.ContentTemplateProperty] = parent[!Pivot.ItemTemplateProperty],
                        }.RegisterInNameScope(scope),
                        new PivotHeader()
                        {
                            Name = "PART_Header"
                        }.RegisterInNameScope(scope)
                    }
                });
        }

        private static IControlTemplate PivotItemTemplate()
        {
            return new FuncControlTemplate<PivotItem>((parent, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = parent[!PivotItem.HeaderProperty],
                    [!ContentPresenter.ContentTemplateProperty] = parent[!PivotItem.HeaderTemplateProperty]
                }.RegisterInNameScope(scope));
        }

        private static void Prepare(Pivot target)
        {
            ApplyTemplate(target);
            target.Measure(Size.Infinity);
            target.Arrange(new Rect(target.DesiredSize));
        }

        private static void ApplyTemplate(Pivot target)
        {
            target.ApplyTemplate();

            target.Presenter.ApplyTemplate();

            foreach (var PivotItem in target.GetLogicalChildren().OfType<PivotItem>())
            {
                PivotItem.Template = PivotItemTemplate();

                PivotItem.ApplyTemplate();

                ((ContentPresenter)PivotItem.Presenter).UpdateChild();
            }
        }

        private class Item
        {
            public Item(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }
    }
}

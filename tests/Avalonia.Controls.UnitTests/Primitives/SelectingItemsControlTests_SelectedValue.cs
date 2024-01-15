using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class SelectingItemsControlTests_SelectedValue
    {
        [Fact]
        public void Setting_SelectedItem_Sets_SelectedValue()
        {
            var items = TestClass.GetItems();
            var sic = new SelectingItemsControl
            {
                ItemsSource = items,
                SelectedValueBinding = new Binding("Name"),
                Template = Template()
            };

            sic.SelectedItem = items[1];

            Assert.Equal(items[1].Name, sic.SelectedValue);
        }

        [Fact]
        public void Setting_SelectedIndex_Sets_SelectedValue()
        {
            var items = TestClass.GetItems();
            var sic = new SelectingItemsControl
            {
                ItemsSource = items,
                SelectedValueBinding = new Binding("Name"),
                Template = Template()
            };

            sic.SelectedIndex = 1;

            Assert.Equal(items[1].Name, sic.SelectedValue);
        }

        [Fact]
        public void Setting_SelectedItems_Sets_SelectedValue()
        {
            var items = TestClass.GetItems();
            var sic = new ListBox
            {
                ItemsSource = items,
                SelectedValueBinding = new Binding("Name"),
                Template = Template()
            };

            sic.SelectedItems = new List<TestClass>
            {
                items[2],
                items[4],
                items[5]
            };

            // When interacting, SelectedItem is the first item in the SelectedItems collection
            // But when set here, it's the last
            Assert.Equal(items[5].Name, sic.SelectedValue);
        }

        [Fact]
        public void Setting_SelectedValue_Sets_SelectedIndex()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var items = TestClass.GetItems();
                var sic = new SelectingItemsControl
                {
                    ItemsSource = items,
                    SelectedValueBinding = new Binding("Name"),
                    Template = Template()
                };

                Prepare(sic);

                sic.SelectedValue = items[2].Name;

                Assert.Equal(2, sic.SelectedIndex);
            }                
        }

        [Fact]
        public void Setting_SelectedValue_Sets_SelectedItem()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var items = TestClass.GetItems();
                var sic = new SelectingItemsControl
                {
                    ItemsSource = items,
                    SelectedValueBinding = new Binding("Name"),
                    Template = Template()
                };

                Prepare(sic);

                sic.SelectedValue = "Item2";

                Assert.Equal(items[2], sic.SelectedItem);
            }                
        }

        [Fact]
        public void Changing_SelectedValueBinding_Updates_SelectedValue()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var items = TestClass.GetItems();
                var sic = new SelectingItemsControl
                {
                    ItemsSource = items,
                    SelectedValueBinding = new Binding("Name"),
                    Template = Template()
                };

                sic.SelectedValue = "Item2";

                sic.SelectedValueBinding = new Binding("AltProperty");

                // Ensure SelectedItem didn't change
                Assert.Equal(items[2], sic.SelectedItem);


                Assert.Equal("Alt2", sic.SelectedValue);
            }                         
        }

        [Fact]
        public void SelectedValue_With_Null_SelectedValueBinding_Is_Item()
        {
            var items = TestClass.GetItems();
            var sic = new SelectingItemsControl
            {
                ItemsSource = items,
                Template = Template()
            };

            sic.SelectedIndex = 1;

            Assert.Equal(items[1], sic.SelectedValue);
        }

        [Fact]
        public void Setting_SelectedValue_Before_Initialize_Should_Retain_Selection()
        {
            var items = TestClass.GetItems();
            var sic = new SelectingItemsControl
            {
                ItemsSource = items,
                Template = Template(),
                SelectedValueBinding = new Binding("Name"),
                SelectedValue = "Item2"
            };

            sic.BeginInit();
            sic.EndInit();

            Assert.Equal(items[2].Name, sic.SelectedValue);
        }

        [Fact]
        public void Setting_SelectedValue_During_Initialize_Should_Take_Priority_Over_Previous_Value()
        {
            var items = TestClass.GetItems();
            var sic = new SelectingItemsControl
            {
                ItemsSource = items,
                Template = Template(),
                SelectedValueBinding = new Binding("Name"),
                SelectedValue = "Item2"
            };

            sic.BeginInit();
            sic.SelectedValue = "Item1";
            sic.EndInit();

            Assert.Equal(items[1].Name, sic.SelectedValue);
        }

        [Fact]
        public void Changing_Items_Should_Clear_SelectedValue()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var items = TestClass.GetItems();
                var sic = new SelectingItemsControl
                {
                    ItemsSource = items,
                    Template = Template(),
                    SelectedValueBinding = new Binding("Name"),
                    SelectedValue = "Item2"
                };

                Prepare(sic);

                sic.ItemsSource = new List<TestClass>
                {
                    new TestClass("NewItem", string.Empty)
                };

                Assert.Equal(null, sic.SelectedValue);
            }
        }

        [Fact]
        public void Setting_SelectedValue_Should_Raise_SelectionChanged_Event()
        {
            // Unlike SelectedIndex/SelectedItem tests, we need the ItemsControl to
            // initialize so that SelectedValue can actually be looked up
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var items = TestClass.GetItems();
                var sic = new SelectingItemsControl
                {
                    ItemsSource = items,
                    Template = Template(),
                    SelectedValueBinding = new Binding("Name"),
                };

                Prepare(sic);

                var called = false;
                sic.SelectionChanged += (s, e) =>
                {
                    Assert.Same(items[2], e.AddedItems.Cast<object>().Single());
                    Assert.Empty(e.RemovedItems);
                    called = true;
                };

                sic.SelectedValue = "Item2";
                Assert.True(called);
            }
        }

        [Fact]
        public void Clearing_SelectedValue_Should_Raise_SelectionChanged_Event()
        {
            var items = TestClass.GetItems();
            var sic = new SelectingItemsControl
            {
                ItemsSource = items,
                Template = Template(),
                SelectedValueBinding = new Binding("Name"),
                SelectedValue = "Item2"
            };

            var called = false;
            sic.SelectionChanged += (s, e) =>
            {
                Assert.Same(items[2], e.RemovedItems.Cast<object>().Single());
                Assert.Empty(e.AddedItems);
                called = true;
            };

            sic.SelectedValue = null;
            Assert.True(called);
        }

        [Fact]
        public void Handles_Null_SelectedItem_When_SelectedValueBinding_Assigned()
        {
            // Issue #11220
            var items = new object[] { null };
            var sic = new SelectingItemsControl
            {
                ItemsSource = items,
                SelectedIndex = 1,
                SelectedValueBinding = new Binding("Name"),
                Template = Template()
            };

            Assert.Null(sic.SelectedValue);
        }

        private static FuncControlTemplate Template()
        {
            return new FuncControlTemplate<SelectingItemsControl>((control, scope) =>
                new ItemsPresenter
                {
                    Name = "itemsPresenter",
                    [~ItemsPresenter.ItemsPanelProperty] = control[~ItemsControl.ItemsPanelProperty],
                }.RegisterInNameScope(scope));
        }

        private static void Prepare(SelectingItemsControl target)
        {
            var root = new TestRoot
            {
                Child = target,
                Width = 100,
                Height = 100,
                Styles =
                {
                    new Style(x => x.Is<SelectingItemsControl>())
                    {
                        Setters =
                        {
                            new Setter(ListBox.TemplateProperty, Template()),
                        },
                    },
                },
            };

            root.LayoutManager.ExecuteInitialLayoutPass();
        }
    }

    internal class TestClass
    {
        public TestClass(string name, string alt)
        {
            Name = name;
            AltProperty = alt;
        }

        public string Name { get; set; }

        public string AltProperty { get; set; }

        public static List<TestClass> GetItems()
        {
            return new List<TestClass>
            {
                new TestClass(null, null),
                new TestClass("Item1", "Alt1"),
                new TestClass("Item2", "Alt2"),
                new TestClass("Item3", "Alt3"),
                new TestClass("Item4", "Alt4"),
                new TestClass("Item5", "Alt5"),
            };
        }
    }
}



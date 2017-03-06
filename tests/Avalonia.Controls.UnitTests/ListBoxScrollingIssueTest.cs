using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.Data;
using Avalonia.UnitTests;
using ReactiveUI;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ListBoxScrollingIssueTest
    {
        [Fact]
        public void ListBox_AfterScroll_Then_New_Items_Should_Be_Correct()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var vm = new ItemsViewModel();
                var lm = LayoutManager.Instance;

                int total = 5;
                var objects = new AvaloniaList<object>(Enumerable.Range(1, total).Select(n => $"foo#{n}"));

                var target = new ListBox();

                target.Bind(ListBox.ItemsProperty, new Binding("Items"));

                target.DataContext = vm;

                target.Width = 100;
                target.Height = 150;
                target.ItemTemplate = new FuncDataTemplate<string>(x =>
                {
                    var tb = new TextBlock { Height = 50 };
                    tb.Bind(TextBlock.TextProperty, new Binding());
                    return tb;
                }, true);

                var root = new Window() { Content = target, Width = 200, Height = 200 };

                lm.ExecuteInitialLayoutPass(root);

                var lc = target.GetLogicalChildren()
                                .OfType<ListBoxItem>()
                                .OrderBy(l => l.DataContext);

                var items = lc.Select(l => (string)l.DataContext);

                vm.Items = objects;

                lm.ExecuteLayoutPass();

                Assert.Equal(3, lc.Count());

                string itemsSummary = "";
                Action<double?> updateVOffset = offset =>
                {
                    if (offset != null)
                    {
                        target.Scroll.Offset = new Vector(0, offset.Value);
                    }
                    lm.ExecuteLayoutPass();
                    itemsSummary = string.Join(",", items);
                };

                //emulate some scrolling with 2 item to be visible
                updateVOffset(0.66);
                updateVOffset(1.034);
                updateVOffset(1.53);

                Assert.Equal(new[] { "foo#2", "foo#3", "foo#4" }, items);

                objects = new AvaloniaList<object>(Enumerable.Range(2, total - 2).Select(n => $"bar#{n}"));

                vm.Items = objects;

                objects.Add($"bar#{total}");

                objects.Insert(0, "bar#1");

                updateVOffset(null);

                //ensure against the current scroll offset we have correct items
                var expected = objects.Skip((int)target.Scroll.Offset.Y).Take(3);
                //we have bar#3,bar#4,bar#5 but we would expect bar#2,bar#3,bar#4 for offset 1
                Assert.Equal(expected, items);

                updateVOffset(0);
                Assert.Equal(new[] { "bar#1", "bar#2", "bar#3" }, items);
                //we have bar#1,bar#3,bar#4

                updateVOffset(1);
                Assert.Equal(new[] { "bar#2", "bar#3", "bar#4" }, items);
                //we have bar#3,bar#4,bar#4

                updateVOffset(2);
                Assert.Equal(new[] { "bar#3", "bar#4", "bar#5" }, items);
                //we have bar#4,bar#4,bar#5
            }
        }

        public class ItemsViewModel : ReactiveObject
        {
            private IEnumerable<object> _items;

            public IEnumerable<object> Items
            {
                get { return _items; }
                set { this.RaiseAndSetIfChanged(ref _items, value); }
            }
        }
    }
}

using System;
using System.Collections;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public sealed class TableViewTests : ScopedTestBase
{
    [Fact]
    public void Container_For_Each_Item_Is_TableViewRow()
    {
        using var app = Start();

        var target = CreateTarget(new[] { "Foo", "Bar", "Baz" });

        Prepare(target);

        var containers = target.GetRealizedContainers().ToList();
        Assert.Equal(3, containers.Count);
        Assert.All(containers, c => Assert.IsType<TableViewRow>(c));
    }

    [Fact]
    public void Row_Has_One_Cell_Per_Column()
    {
        using var app = Start();

        var target = CreateTarget(new[] { "Foo" });
        target.Columns.Add(new TableViewColumn());
        target.Columns.Add(new TableViewColumn());
        target.Columns.Add(new TableViewColumn());

        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        var presenter = GetRowPresenter(row);
        Assert.Equal(3, presenter.Children.Count);
        Assert.All(presenter.Children, c => Assert.IsType<TableViewCell>(c));
    }

    [Fact]
    public void Adding_Column_Adds_Cell_To_Realized_Rows_And_Headers()
    {
        using var app = Start();

        var target = CreateTarget(new[] { "Foo" });
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

        Prepare(target);

        var rowPresenter = GetRowPresenter((TableViewRow)target.GetRealizedContainers().Single());
        Assert.Equal(2, rowPresenter.Children.Count);

        var headersPresenter = GetColumnHeadersPresenter(target);
        Assert.Equal(2, headersPresenter.Children.Count);

        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

        Assert.Equal(3, rowPresenter.Children.Count);
        Assert.Equal(3, headersPresenter.Children.Count);
    }

    [Fact]
    public void Removing_Column_Removes_Cell_From_Realized_Rows_And_Headers()
    {
        using var app = Start();

        var target = CreateTarget(new[] { "Foo" });
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

        Prepare(target);

        var rowPresenter = GetRowPresenter((TableViewRow)target.GetRealizedContainers().Single());
        Assert.Equal(3, rowPresenter.Children.Count);

        var headersPresenter = GetColumnHeadersPresenter(target);
        Assert.Equal(3, headersPresenter.Children.Count);

        target.Columns.RemoveAt(2);

        Assert.Equal(2, rowPresenter.Children.Count);
        Assert.Equal(2, headersPresenter.Children.Count);
    }

    [Fact]
    public void Changing_Column_Width_Does_Not_Recreate_Cells()
    {
        using var app = Start();

        var column = new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) };
        var target = CreateTarget(new[] { "Foo" });
        target.Columns.Add(column);
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

        Prepare(target, width: 200);

        var presenter = GetRowPresenter((TableViewRow)target.GetRealizedContainers().Single());
        var cellsBefore = presenter.Children.ToArray();

        column.Width = new GridLength(2, GridUnitType.Star);
        Layout(target);

        var cellsAfter = presenter.Children.ToArray();
        Assert.Equal(cellsBefore, cellsAfter);
    }

    [Fact]
    public void Changing_Column_Width_Invalidates_Row_Presenter_Measure()
    {
        using var app = Start();

        var column = new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) };
        var target = CreateTarget(new[] { "Foo" });
        target.Columns.Add(column);
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

        Prepare(target, width: 200);

        var presenter = GetRowPresenter((TableViewRow)target.GetRealizedContainers().Single());
        Assert.True(presenter.IsMeasureValid);

        column.Width = new GridLength(2, GridUnitType.Star);

        Assert.False(presenter.IsMeasureValid);
    }

    [Fact]
    public void Changing_Column_Width_Updates_ActualWidth_After_Layout()
    {
        using var app = Start();

        var column = new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) };
        var target = CreateTarget(new[] { "Foo" });
        target.Columns.Add(column);
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

        Prepare(target, width: 200);

        Assert.Equal(100, target.Columns[0].ActualWidth);
        Assert.Equal(100, target.Columns[1].ActualWidth);

        column.Width = new GridLength(3, GridUnitType.Star);
        Layout(target);

        Assert.Equal(150, target.Columns[0].ActualWidth);
        Assert.Equal(50, target.Columns[1].ActualWidth);
    }

    [Fact]
    public void Replacing_Columns_Collection_Updates_Realized_Rows_And_Headers()
    {
        using var app = Start();

        var target = CreateTarget(new[] { "Foo" });
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

        Prepare(target);

        var rowPresenter = GetRowPresenter((TableViewRow)target.GetRealizedContainers().Single());
        Assert.Equal(2, rowPresenter.Children.Count);

        var headersPresenter = GetColumnHeadersPresenter(target);
        Assert.Equal(2, headersPresenter.Children.Count);

        target.Columns =
        [
            new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) },
            new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) },
            new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) }
        ];

        Assert.Equal(3, rowPresenter.Children.Count);
        Assert.Equal(3, headersPresenter.Children.Count);
    }

    [Fact]
    public void Cell_Content_Defaults_To_Row_Item_When_No_Template_Or_Binding()
    {
        using var app = Start();

        var item = new Person("Alice");
        var target = CreateTarget(new[] { item });
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        var firstCell = (TableViewCell)GetRowPresenter(row).Children[0];

        Assert.Same(item, firstCell.Content);
    }

    [Fact]
    public void Cell_Uses_Column_Binding()
    {
        using var app = Start();

        var target = CreateTarget(new[] { new Person("Alice"), new Person("Bob") });
        target.Columns.Add(new TableViewColumn
        {
            Width = new GridLength(1, GridUnitType.Star),
            Binding = new ReflectionBinding(nameof(Person.Name))
        });
        target.Columns.Add(new TableViewColumn
        {
            Width = new GridLength(1, GridUnitType.Star)
        });

        Prepare(target);

        var rows = target.GetRealizedContainers().Cast<TableViewRow>().ToArray();
        var firstCell = (TableViewCell)GetRowPresenter(rows[0]).Children[0];
        var secondCell = (TableViewCell)GetRowPresenter(rows[1]).Children[0];

        Assert.Equal("Alice", firstCell.Content);
        Assert.Equal("Bob", secondCell.Content);
    }

    [Fact]
    public void Cell_Uses_Column_CellTemplate()
    {
        using var app = Start();

        var template = new FuncDataTemplate<object>((_, _) => new TextBlock());
        var item = new Person("Alice");
        var target = CreateTarget(new[] { item });
        target.Columns.Add(new TableViewColumn
        {
            Width = new GridLength(1, GridUnitType.Star),
            CellTemplate = template,
        });
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        var firstCell = (TableViewCell)GetRowPresenter(row).Children[0];

        Assert.Same(template, firstCell.ContentTemplate);
        Assert.Same(item, firstCell.Content);
    }

    [Fact]
    public void Cell_Uses_Column_CellTheme()
    {
        using var app = Start();

        var cellTheme = new ControlTheme(typeof(TableViewCell));
        var target = CreateTarget(new[] { "Foo" });
        target.Columns.Add(new TableViewColumn
        {
            Width = new GridLength(1, GridUnitType.Star),
            CellTheme = cellTheme,
        });
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        var firstCell = (TableViewCell)GetRowPresenter(row).Children[0];
        var secondCell = (TableViewCell)GetRowPresenter(row).Children[1];

        Assert.Same(cellTheme, firstCell.Theme);
        Assert.Null(secondCell.Theme);
    }

    [Fact]
    public void Cell_Column_Property_Is_Set_To_Owning_Column()
    {
        using var app = Start();

        var column0 = new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) };
        var column1 = new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) };
        var target = CreateTarget(new[] { "Foo" });
        target.Columns.Add(column0);
        target.Columns.Add(column1);

        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        var firstCell = (TableViewCell)GetRowPresenter(row).Children[0];
        var secondCell = (TableViewCell)GetRowPresenter(row).Children[1];

        Assert.Same(column0, firstCell.Column);
        Assert.Same(column1, secondCell.Column);
    }

    private static IDisposable Start()
        => UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

    private static TableView CreateTarget(IEnumerable items)
        => new()
        {
            Template = TableViewTemplate(),
            ItemContainerTheme = TableViewRowTheme(),
            ItemsSource = items
        };

    private static TableViewRowPresenter GetRowPresenter(TableViewRow row)
    {
        var presenter = row.GetVisualDescendants()
            .OfType<TableViewRowPresenter>()
            .FirstOrDefault();
        Assert.NotNull(presenter);
        return presenter;
    }

    private static TableViewColumnHeadersPresenter GetColumnHeadersPresenter(TableView target)
    {
        var presenter = target.GetVisualDescendants()
            .OfType<TableViewColumnHeadersPresenter>()
            .FirstOrDefault();
        Assert.NotNull(presenter);
        return presenter;
    }

    private static void Prepare(TableView target, double width = 300, double height = 200)
    {
        target.Width = width;
        target.Height = height;
        var root = new TestRoot(target);
        root.LayoutManager.ExecuteInitialLayoutPass();
    }

    private static void Layout(Control c)
    {
        c.GetLayoutManager()?.ExecuteLayoutPass();
    }

    private static FuncControlTemplate TableViewTemplate()
        => new FuncControlTemplate<TableView>((parent, scope) =>
            new DockPanel
            {
                Children =
                {
                    new TableViewColumnHeadersPresenter
                    {
                        [DockPanel.DockProperty] = Dock.Top
                    },
                    new ScrollViewer
                    {
                        Name = "PART_ScrollViewer",
                        Template = ScrollViewerTemplate(),
                        Content = new ItemsPresenter
                        {
                            Name = "PART_ItemsPresenter",
                            [~ItemsPresenter.ItemsPanelProperty] =
                                parent.GetObservable(ItemsControl.ItemsPanelProperty).ToBinding()
                        }.RegisterInNameScope(scope)
                    }.RegisterInNameScope(scope)
                }
            });

    private static FuncControlTemplate ScrollViewerTemplate()
        => new FuncControlTemplate<ScrollViewer>((_, scope) =>
            new Panel
            {
                Children =
                {
                    new ScrollContentPresenter
                    {
                        Name = "PART_ContentPresenter"
                    }.RegisterInNameScope(scope)
                }
            });

    private static ControlTheme TableViewRowTheme()
        => new(typeof(TableViewRow))
        {
            Setters =
            {
                new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<TableViewRow>((_, scope) =>
                    new TableViewRowPresenter
                    {
                        Name = "PART_RowPresenter"
                    }.RegisterInNameScope(scope)))
            }
        };

    private class Person
    {
        public Person(string name) => Name = name;
        public string Name { get; }
    }
}

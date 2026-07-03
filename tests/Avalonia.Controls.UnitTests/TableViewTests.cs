using System;
using System.Collections;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.LogicalTree;
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
        var cells = GetCellsPresenter(row).Children;
        Assert.Equal(3, cells.Count);
        Assert.All(cells, c => Assert.IsType<TableViewCell>(c));

        // Cells must also be part of the row's logical tree.
        var logicalChildren = row.GetLogicalChildren().ToArray();
        Assert.Equal(cells, logicalChildren);
    }

    [Fact]
    public void Column_TableView_Is_Set_When_Added_And_Unset_When_Removed()
    {
        using var app = Start();

        var target = CreateTarget(new[] { "Foo" });
        var column = new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) };
        target.Columns.Add(column);

        Prepare(target);

        Assert.Same(target, column.TableView);

        target.Columns.Remove(column);

        Assert.Null(column.TableView);
    }

    [Fact]
    public void Column_Cannot_Belong_To_Two_TableViews()
    {
        using var app = Start();

        var column = new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) };

        var first = CreateTarget(new[] { "Foo" });
        first.Columns.Add(column);
        Prepare(first);

        Assert.Same(first, column.TableView);

        var second = CreateTarget(new[] { "Bar" });
        second.Columns.Add(column);

        Assert.Throws<InvalidOperationException>(() => Prepare(second));
    }

    [Fact]
    public void Adding_Column_Adds_Cell_To_Realized_Rows_And_Headers()
    {
        using var app = Start();

        var target = CreateTarget(new[] { "Foo" });
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        var cellsPresenter = GetCellsPresenter(row);
        Assert.Equal(2, cellsPresenter.Children.Count);

        var headersPresenter = GetColumnHeadersPresenter(target);
        Assert.Equal(2, headersPresenter.Children.Count);

        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

        Assert.Equal(3, cellsPresenter.Children.Count);
        Assert.Equal(3, headersPresenter.Children.Count);

        // The newly added cell must also be part of the row's logical tree.
        var logicalChildren = row.GetLogicalChildren().ToArray();
        Assert.Equal(cellsPresenter.Children, logicalChildren);
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

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        var cellsPresenter = GetCellsPresenter(row);
        Assert.Equal(3, cellsPresenter.Children.Count);

        var headersPresenter = GetColumnHeadersPresenter(target);
        Assert.Equal(3, headersPresenter.Children.Count);

        var removedCell = (TableViewCell)cellsPresenter.Children[2];

        target.Columns.RemoveAt(2);

        Assert.Equal(2, cellsPresenter.Children.Count);
        Assert.Equal(2, headersPresenter.Children.Count);

        // The removed cell must also be detached from the row's logical tree, while the remaining cells stay in it.
        var logicalChildren = row.GetLogicalChildren().ToArray();
        Assert.DoesNotContain(removedCell, logicalChildren);
        Assert.Equal(cellsPresenter.Children, logicalChildren);
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

        var presenter = GetCellsPresenter((TableViewRow)target.GetRealizedContainers().Single());
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

        var presenter = GetCellsPresenter((TableViewRow)target.GetRealizedContainers().Single());
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

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        var cellsPresenter = GetCellsPresenter(row);
        Assert.Equal(2, cellsPresenter.Children.Count);

        var headersPresenter = GetColumnHeadersPresenter(target);
        Assert.Equal(2, headersPresenter.Children.Count);

        var oldCells = cellsPresenter.Children.ToArray();

        target.Columns =
        [
            new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) },
            new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) },
            new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) }
        ];

        Assert.Equal(3, cellsPresenter.Children.Count);
        Assert.Equal(3, headersPresenter.Children.Count);

        // The new cells must be in the row's logical tree, and the old ones gone.
        var logicalChildren = row.GetLogicalChildren().ToArray();
        Assert.Equal(cellsPresenter.Children, logicalChildren);
        Assert.All(oldCells, cell => Assert.DoesNotContain(cell, logicalChildren));
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
        var firstCell = (TableViewCell)GetCellsPresenter(row).Children[0];

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
        var firstCell = (TableViewCell)GetCellsPresenter(rows[0]).Children[0];
        var secondCell = (TableViewCell)GetCellsPresenter(rows[1]).Children[0];

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
            CellTemplate = template
        });
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        var firstCell = (TableViewCell)GetCellsPresenter(row).Children[0];

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
            CellTheme = cellTheme
        });
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        var firstCell = (TableViewCell)GetCellsPresenter(row).Children[0];
        var secondCell = (TableViewCell)GetCellsPresenter(row).Children[1];

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
        var firstCell = (TableViewCell)GetCellsPresenter(row).Children[0];
        var secondCell = (TableViewCell)GetCellsPresenter(row).Children[1];

        Assert.Same(column0, firstCell.Column);
        Assert.Same(column1, secondCell.Column);
    }

    [Fact]
    public void Cell_Uses_Column_HorizontalContentAlignment()
    {
        using var app = Start();

        var target = CreateTarget(new[] { "Foo" });
        target.Columns.Add(new TableViewColumn
        {
            Width = new GridLength(1, GridUnitType.Star),
            HorizontalContentAlignment = HorizontalAlignment.Right
        });
        target.Columns.Add(new TableViewColumn { Width = new GridLength(1, GridUnitType.Star) });

        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        var firstCell = (TableViewCell)GetCellsPresenter(row).Children[0];
        var secondCell = (TableViewCell)GetCellsPresenter(row).Children[1];

        Assert.Equal(HorizontalAlignment.Right, firstCell.HorizontalContentAlignment);
        Assert.Equal(HorizontalAlignment.Left, secondCell.HorizontalContentAlignment);
    }

    [Fact]
    public void Changing_Column_Properties_Updates_Existing_Headers_And_Cells()
    {
        using var app = Start();

        var cellThemeA = new ControlTheme(typeof(TableViewCell));
        var cellThemeB = new ControlTheme(typeof(TableViewCell));
        var headerThemeA = new ControlTheme(typeof(TableViewColumnHeader));
        var headerThemeB = new ControlTheme(typeof(TableViewColumnHeader));
        var headerTemplateA = new FuncDataTemplate<object>((_, _) => new TextBlock());
        var headerTemplateB = new FuncDataTemplate<object>((_, _) => new TextBlock());

        var item = new Person("Alice", "Ally");
        var column = new TableViewColumn
        {
            Width = new GridLength(1, GridUnitType.Star),
            CellTheme = cellThemeA,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Binding = new ReflectionBinding(nameof(Person.Name)),
            Header = "H1",
            HeaderTheme = headerThemeA,
            HeaderTemplate = headerTemplateA
        };
        var target = CreateTarget(new[] { item });
        target.Columns.Add(column);

        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        var cell = (TableViewCell)GetCellsPresenter(row).Children[0];
        var header = (TableViewColumnHeader)GetColumnHeadersPresenter(target).Children[0];

        Assert.Same(cellThemeA, cell.Theme);
        Assert.Equal(HorizontalAlignment.Left, cell.HorizontalContentAlignment);
        Assert.Null(cell.ContentTemplate);
        Assert.Equal("Alice", cell.Content);

        Assert.Same(headerThemeA, header.Theme);
        Assert.Equal(HorizontalAlignment.Left, header.HorizontalContentAlignment);
        Assert.Same(headerTemplateA, header.ContentTemplate);
        Assert.Equal("H1", header.Content);

        // Mutating the column after the cell and header were built should update both.
        // Switch the Binding first to confirm it's reflected in the cell content.
        column.CellTheme = cellThemeB;
        column.HorizontalContentAlignment = HorizontalAlignment.Right;
        column.Binding = new ReflectionBinding(nameof(Person.Nickname));
        column.Header = "H2";
        column.HeaderTheme = headerThemeB;
        column.HeaderTemplate = headerTemplateB;

        Assert.Same(cellThemeB, cell.Theme);
        Assert.Equal(HorizontalAlignment.Right, cell.HorizontalContentAlignment);
        Assert.Null(cell.ContentTemplate);
        Assert.Equal("Ally", cell.Content);

        Assert.Same(headerThemeB, header.Theme);
        Assert.Equal(HorizontalAlignment.Right, header.HorizontalContentAlignment);
        Assert.Same(headerTemplateB, header.ContentTemplate);
        Assert.Equal("H2", header.Content);

        // CellTemplate takes priority over Binding: the row item flows through the template.
        var cellTemplateB = new FuncDataTemplate<object>((_, _) => new TextBlock());
        column.CellTemplate = cellTemplateB;

        Assert.Same(cellTemplateB, cell.ContentTemplate);
        Assert.Same(item, cell.Content);
    }

    [Fact]
    public void Re_Templating_Row_Detaches_Old_Cells_And_Rebuilds_New_Cells()
    {
        using var app = Start();

        var target = CreateTarget(new[] { "Foo" });
        target.Columns.Add(new TableViewColumn());
        target.Columns.Add(new TableViewColumn());

        Prepare(target);

        var row = (TableViewRow)target.GetRealizedContainers().Single();
        var oldCells = GetCellsPresenter(row).Children.ToArray();
        Assert.Equal(2, oldCells.Length);
        Assert.Equal(oldCells, row.GetLogicalChildren());

        row.Template = RowTemplate();
        row.ApplyTemplate();

        var newCells = GetCellsPresenter(row).Children.ToArray();
        Assert.Equal(2, newCells.Length);

        Assert.All(oldCells, cell => Assert.Null(cell.Parent));
        Assert.Equal(newCells, row.GetLogicalChildren());
    }

    [Fact]
    public void CanResizeColumns_Defaults_To_True()
    {
        Assert.True(new TableView().CanResizeColumns);
    }

    [Theory]
    [InlineData(true, null, true)] // inherits the table's value
    [InlineData(true, true, true)] // column opts in
    [InlineData(true, false, false)] // column opts out
    [InlineData(false, null, false)] // inherits the table's value
    [InlineData(false, true, true)] // column opts in
    [InlineData(false, false, false)] // column opts out
    public void Column_CanEffectivelyResize_Combines_TableView_And_Column_Settings(
        bool canResizeColumns,
        bool? canResize,
        bool expected)
    {
        using var app = Start();

        var target = CreateTarget(new[] { "Foo" });
        target.CanResizeColumns = canResizeColumns;
        var column = new TableViewColumn { CanResize = canResize };
        target.Columns.Add(column);

        Prepare(target);

        Assert.Equal(expected, column.CanEffectivelyResize);
    }

    [Fact]
    public void Column_CanEffectivelyResize_Updates_When_TableView_CanResizeColumns_Changes()
    {
        using var app = Start();

        var target = CreateTarget(new[] { "Foo" });
        var column = new TableViewColumn();
        target.Columns.Add(column);

        Prepare(target);

        Assert.True(column.CanEffectivelyResize);

        target.CanResizeColumns = false;
        Assert.False(column.CanEffectivelyResize);

        target.CanResizeColumns = true;
        Assert.True(column.CanEffectivelyResize);
    }

    [Fact]
    public void Column_CanEffectivelyResize_Updates_When_Column_CanResize_Changes()
    {
        using var app = Start();

        var target = CreateTarget(new[] { "Foo" });
        target.CanResizeColumns = false;
        var column = new TableViewColumn();
        target.Columns.Add(column);

        Prepare(target);

        Assert.False(column.CanEffectivelyResize);

        column.CanResize = true;
        Assert.True(column.CanEffectivelyResize);

        column.CanResize = null;
        Assert.False(column.CanEffectivelyResize);
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

    private static TableViewCellsPresenter GetCellsPresenter(TableViewRow row)
    {
        var presenter = row.GetVisualDescendants()
            .OfType<TableViewCellsPresenter>()
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

    private static void Layout(Control control)
        => control.GetLayoutManager()?.ExecuteLayoutPass();

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
                new Setter(TemplatedControl.TemplateProperty, RowTemplate())
            }
        };

    private static FuncControlTemplate<TableViewRow> RowTemplate()
        => new((_, scope) =>
            new TableViewCellsPresenter
            {
                Name = "PART_CellsPresenter"
            }.RegisterInNameScope(scope));

    private class Person
    {
        public Person(string name, string? nickname = null)
        {
            Name = name;
            Nickname = nickname;
        }

        public string Name { get; }
        public string? Nickname { get; }
    }
}

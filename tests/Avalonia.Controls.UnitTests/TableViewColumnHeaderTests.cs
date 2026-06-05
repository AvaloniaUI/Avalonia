using System;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public sealed class TableViewColumnHeaderTests : ScopedTestBase
{
    [Fact]
    public void Dragging_Resizer_Sets_Column_Width_To_Pixel_Value()
    {
        using var app = Start();

        var column = new TableViewColumn
        {
            Width = new GridLength(1, GridUnitType.Star),
            ActualWidth = 100
        };
        var header = new TableViewColumnHeader
        {
            Column = column,
            Theme = TableViewColumnHeaderTheme(0)
        };
        var root = new TestRoot { Child = header };
        root.LayoutManager.ExecuteInitialLayoutPass();

        var thumb = header.GetVisualDescendants().OfType<Thumb>().Single();
        thumb.RaiseEvent(new VectorEventArgs
        {
            RoutedEvent = Thumb.DragDeltaEvent,
            Vector = new Vector(15, 0)
        });

        Assert.Equal(new GridLength(115), column.Width);
    }

    [Fact]
    public void Dragging_Resizer_Clamps_Column_Width_To_Resizer_Width()
    {
        using var app = Start();

        var column = new TableViewColumn
        {
            Width = new GridLength(50),
            ActualWidth = 50
        };
        var header = new TableViewColumnHeader
        {
            Column = column,
            Theme = TableViewColumnHeaderTheme(6)
        };
        var root = new TestRoot { Child = header };
        root.LayoutManager.ExecuteInitialLayoutPass();

        var thumb = header.GetVisualDescendants().OfType<Thumb>().Single();
        thumb.RaiseEvent(new VectorEventArgs
        {
            RoutedEvent = Thumb.DragDeltaEvent,
            Vector = new Vector(-500, 0)
        });

        Assert.Equal(new GridLength(6), column.Width);
    }

    private static IDisposable Start()
        => UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);

    private static ControlTheme TableViewColumnHeaderTheme(int resizerWidth)
        => new(typeof(TableViewColumnHeader))
        {
            Setters =
            {
                new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<TableViewColumnHeader>((_, scope) =>
                    new Thumb
                    {
                        Name = "PART_Resizer",
                        Width = resizerWidth
                    }.RegisterInNameScope(scope)))
            }
        };
}

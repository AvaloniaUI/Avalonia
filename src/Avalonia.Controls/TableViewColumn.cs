using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Controls;

/// <summary>
/// Defines a column in a <see cref="TableView"/>.
/// </summary>
public class TableViewColumn : StyledElement, IHeadered
{
    /// <summary>
    /// Defines the <see cref="Header"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<TableViewColumn, object?>(nameof(Header));

    /// <summary>
    /// Defines the <see cref="HeaderTemplate"/> property.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<TableViewColumn, IDataTemplate?>(nameof(HeaderTemplate));

    /// <summary>
    /// Defines the <see cref="Width"/> property.
    /// </summary>
    public static readonly StyledProperty<GridLength> WidthProperty =
        AvaloniaProperty.Register<TableViewColumn, GridLength>(nameof(Width), new GridLength(1, GridUnitType.Star));

    /// <summary>
    /// Defines the <see cref="CellTheme"/> property.
    /// </summary>
    public static readonly StyledProperty<ControlTheme?> CellThemeProperty =
        AvaloniaProperty.Register<TableViewColumn, ControlTheme?>(nameof(CellTheme));

    /// <summary>
    /// Defines the <see cref="CellTemplate"/> property.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> CellTemplateProperty =
        AvaloniaProperty.Register<TableViewColumn, IDataTemplate?>(nameof(CellTemplate));

    /// <summary>
    /// Defines the <see cref="Binding" /> property.
    /// </summary>
    public static readonly StyledProperty<BindingBase?> BindingProperty =
        AvaloniaProperty.Register<TableViewColumn, BindingBase?>(nameof(Binding));

    /// <summary>
    /// Gets or sets the column header content.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets the data template used to display the header.
    /// </summary>
    public IDataTemplate? HeaderTemplate
    {
        get => GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }


    /// <summary>
    /// Gets or sets the column width. Supports pixel, star (*) and auto sizing.
    /// </summary>
    public GridLength Width
    {
        get => GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the theme to apply to the cells.
    /// </summary>
    public ControlTheme? CellTheme
    {
        get => GetValue(CellThemeProperty);
        set => SetValue(CellThemeProperty, value);
    }

    /// <summary>
    /// Gets or sets the data template used to display cell content.
    /// When set, the whole row data item is passed as the template's data context.
    /// </summary>
    /// <remarks>This property takes priority over <see cref="Binding"/>.</remarks>
    [InheritDataTypeFromItems(nameof(TableView.ItemsSource), AncestorType = typeof(TableView))]
    public IDataTemplate? CellTemplate
    {
        get => GetValue(CellTemplateProperty);
        set => SetValue(CellTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets a binding that retrieves the cell value from the row data item.
    /// Use this for simple property display, e.g. <c>Binding="{Binding Name}"</c>.
    /// </summary>
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(TableView.ItemsSource), AncestorType = typeof(TableView))]
    public BindingBase? Binding
    {
        get => GetValue(BindingProperty);
        set => SetValue(BindingProperty, value);
    }

    /// <summary>
    /// Gets the column's actual rendered width, as computed by the presenter.
    /// </summary>
    internal double ActualWidth { get; set; } = double.NaN;
}

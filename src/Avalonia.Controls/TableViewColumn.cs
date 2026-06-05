using System.Text;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
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
    /// Defines the <see cref="CanResize"/> property.
    /// </summary>
    public static readonly StyledProperty<bool?> CanResizeProperty =
        AvaloniaProperty.Register<TableViewColumn, bool?>(nameof(CanResize));

    /// <summary>
    /// Defines the <see cref="HorizontalContentAlignment"/> property.
    /// </summary>
    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        ContentControl.HorizontalContentAlignmentProperty.AddOwner<TableViewColumn>(
            new StyledPropertyMetadata<HorizontalAlignment>(defaultValue: HorizontalAlignment.Left));

    /// <summary>
    /// Defines the <see cref="TableView"/> property.
    /// </summary>
    public static readonly DirectProperty<TableViewColumn, TableView?> TableViewProperty =
        AvaloniaProperty.RegisterDirect<TableViewColumn, TableView?>(nameof(TableView), o => o.TableView);

    /// <summary>
    /// Defines the <see cref="ActualWidth"/> property.
    /// </summary>
    public static readonly DirectProperty<TableViewColumn, double> ActualWidthProperty =
        AvaloniaProperty.RegisterDirect<TableViewColumn, double>(nameof(ActualWidth), o => o.ActualWidth);

    /// <summary>
    /// Defines the <see cref="CanEffectivelyResize"/> property.
    /// </summary>
    public static readonly DirectProperty<TableViewColumn, bool> CanEffectivelyResizeProperty =
        AvaloniaProperty.RegisterDirect<TableViewColumn, bool>(nameof(CanEffectivelyResize), o => o.CanEffectivelyResize);

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
    /// Gets or sets whether the column can be resized.
    /// Set to null to use the value from <see cref="TableView.CanResizeColumns"/>.
    /// The default is null.
    /// </summary>
    public bool? CanResize
    {
        get => GetValue(CanResizeProperty);
        set => SetValue(CanResizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the horizontal alignment of the content within a cell.
    /// </summary>
    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    /// <summary>
    /// Gets the <see cref="TableView"/> associated with this column.
    /// </summary>
    public TableView? TableView
    {
        get;
        internal set
        {
            if (SetAndRaise(TableViewProperty, ref field, value))
                UpdateCanEffectivelyResize();
        }
    }

    /// <summary>
    /// Gets the actual width of the column, in device independent pixels.
    /// If the column hasn't yet been measured, returns <see cref="double.NaN"/>.
    /// </summary>
    public double ActualWidth
    {
        get;
        internal set => SetAndRaise(ActualWidthProperty, ref field, value);
    } = double.NaN;

    /// <summary>
    /// Gets whether the column can be effectively resized.
    /// The value of this property depends on both <see cref="CanResize"/> and
    /// <see cref="Avalonia.Controls.TableView.CanResizeColumns"/>.
    /// </summary>
    public bool CanEffectivelyResize
    {
        get;
        internal set => SetAndRaise(CanEffectivelyResizeProperty, ref field, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WidthProperty)
            TableView?.OnColumnsSizeChanged();
        else if (change.Property == CanResizeProperty)
            UpdateCanEffectivelyResize();
        else if (change.Property == CellThemeProperty ||
                 change.Property == CellTemplateProperty ||
                 change.Property == BindingProperty ||
                 change.Property == HorizontalContentAlignmentProperty)
        {
            TableView?.RefreshColumnCells(this);
        }
    }

    internal void UpdateCanEffectivelyResize()
        => CanEffectivelyResize = CanResize ?? TableView?.CanResizeColumns ?? true;

    internal override void BuildDebugDisplay(StringBuilder builder, bool includeContent)
    {
        base.BuildDebugDisplay(builder, includeContent);

        if (includeContent)
        {
            DebugDisplayHelper.AppendOptionalValue(builder, nameof(Header), Header, includeContent);
        }
    }
}

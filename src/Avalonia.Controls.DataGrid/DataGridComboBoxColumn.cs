using System;
using System.Collections;
using Avalonia.Collections;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Controls;

public class DataGridComboBoxColumn : DataGridBoundColumn
{
    /// <summary>
    /// Defines the <see cref="DisplayMemberBinding" /> property
    /// </summary>
    public static readonly StyledProperty<IBinding> DisplayMemberBindingProperty =
        ItemsControl.DisplayMemberBindingProperty.AddOwner<DataGridComboBoxColumn>();

    /// <summary>
    /// Defines the <see cref="ItemsSource"/> property.
    /// </summary>
    public static readonly StyledProperty<IEnumerable> ItemsSourceProperty =
        ItemsControl.ItemsSourceProperty.AddOwner<DataGridComboBoxColumn>();

    /// <summary>
    /// Defines the <see cref="ItemTemplate"/> property.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
        ItemsControl.ItemTemplateProperty.AddOwner<DataGridComboBoxColumn>();

    /// <summary>
    /// Defines the <see cref="SelectedValue"/> property
    /// </summary>
    public static readonly StyledProperty<IBinding> SelectedValueProperty =
        //we cant use SelectingItemsControl.SelectedValue here because we need the binding to check for readonly status
        AvaloniaProperty.Register<DataGridComboBoxColumn, IBinding>(nameof(SelectedValue));

    /// <summary>
    /// Defines the <see cref="SelectedValueBinding"/> property
    /// </summary>
    public static readonly StyledProperty<IBinding> SelectedValueBindingProperty =
        SelectingItemsControl.SelectedValueBindingProperty.AddOwner<DataGridComboBoxColumn>();

    /// <summary>
    /// Defines the <see cref="SelectionBoxItemTemplate"/> property.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate> SelectionBoxItemTemplateProperty =
        ComboBox.SelectionBoxItemTemplateProperty.AddOwner<DataGridComboBoxColumn>();

    /// <inheritdoc cref="ItemsControl.DisplayMemberBinding"/>
    [AssignBinding, InheritDataTypeFromItems(nameof(ItemsSource))]
    public IBinding DisplayMemberBinding
    {
        get => GetValue(DisplayMemberBindingProperty);
        set => SetValue(DisplayMemberBindingProperty, value);
    }


    /// <inheritdoc cref="ItemsControl.ItemsSource"/>
    public IEnumerable ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <inheritdoc cref="ItemsControl.ItemTemplate"/>
    [InheritDataTypeFromItems(nameof(DataGrid.ItemsSource), AncestorType = typeof(DataGrid))]
    public IDataTemplate ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>
    /// The binding used to get the value of the selected item
    /// </summary>
    [AssignBinding, InheritDataTypeFromItems(nameof(DataGrid.ItemsSource), AncestorType = typeof(DataGrid))]
    public IBinding SelectedValue
    {
        get => GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    /// <summary>
    /// A binding used to get the value of an item selected in the combobox
    /// </summary>
    [AssignBinding, InheritDataTypeFromItems(nameof(ItemsSource), AncestorType = typeof(DataGridComboBoxColumn))]
    public IBinding SelectedValueBinding
    {
        get => GetValue(SelectedValueBindingProperty);
        set => SetValue(SelectedValueBindingProperty, value);
    }

    /// <summary>
    /// Gets or sets the data template used to display the item in the combo box (not the dropdown) 
    /// </summary>
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public IDataTemplate SelectionBoxItemTemplate
    {
        get => GetValue(SelectionBoxItemTemplateProperty);
        set => SetValue(SelectionBoxItemTemplateProperty, value);
    }

    [AssignBinding, InheritDataTypeFromItems(nameof(DataGrid.ItemsSource), AncestorType = typeof(DataGrid))]
    public virtual IBinding SelectedItemBinding
    {
        get => Binding;
        set => Binding = value;
    }


    private readonly Lazy<ControlTheme> _cellComboBoxTheme;

    public DataGridComboBoxColumn()
    {
        BindingTarget = SelectingItemsControl.SelectedItemProperty;

        _cellComboBoxTheme = new Lazy<ControlTheme>(() =>
                OwningGrid.TryFindResource("DataGridCellComboBoxTheme", out var theme) ? (ControlTheme)theme : null);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DisplayMemberBindingProperty
            || change.Property == ItemsSourceProperty
            || change.Property == ItemTemplateProperty
            || change.Property == SelectedValueProperty
            || change.Property == SelectedValueBindingProperty
            || change.Property == SelectionBoxItemTemplateProperty)
        {
            NotifyPropertyChanged(change.Property.Name);
        }

        //if using the SelectedValue binding then the combobox needs to be bound using the selected value
        //otherwise use the default SelectedItem
        if (change.Property == SelectedValueProperty)
            BindingTarget = change.NewValue == null
                ? SelectingItemsControl.SelectedItemProperty
                : SelectingItemsControl.SelectedValueProperty;
    }

    /// <summary>
    /// Gets a <see cref="T:Avalonia.Controls.ComboBox" /> control that is bound to the column's <see cref="SelectedItemBinding"/> property value.
    /// </summary>
    /// <param name="cell">The cell that will contain the generated element.</param>
    /// <param name="dataItem">The data item represented by the row that contains the intended cell.</param>
    /// <returns>A new <see cref="T:Avalonia.Controls.ComboBox" /> control that is bound to the column's <see cref="SelectedItemBinding"/> property value.</returns>
    protected override Control GenerateEditingElementDirect(DataGridCell cell, object dataItem)
    {
        ComboBox comboBox = new ComboBox
        {
            Name = "CellComboBox"
        };

        if (_cellComboBoxTheme.Value is { } theme)
            comboBox.Theme = theme;

        SyncProperties(comboBox);

        return comboBox;
    }

    protected override Control GenerateElement(DataGridCell cell, object dataItem)
    {
        ComboBox comboBox = new ComboBox
        {
            Name = "DisplayValueComboBox",
            IsHitTestVisible = false
        };

        if (_cellComboBoxTheme.Value is { } theme)
            comboBox.Theme = theme;

        SyncProperties(comboBox);

        if (Binding != null && dataItem != DataGridCollectionView.NewItemPlaceholder)
            comboBox.Bind(BindingTarget, Binding);

        return comboBox;
    }

    protected override object PrepareCellForEdit(Control editingElement, RoutedEventArgs editingEventArgs)
    {
        if (editingElement is ComboBox comboBox)
        {
            comboBox.IsDropDownOpen = true;
            if (BindingTarget == SelectingItemsControl.SelectedValueProperty)
                return comboBox.SelectedValue;

            return comboBox.SelectedItem;
        }
        return null;
    }

    private void SyncProperties(ComboBox comboBox)
    {
        DataGridHelper.SyncColumnProperty(this, comboBox, DisplayMemberBindingProperty);
        DataGridHelper.SyncColumnProperty(this, comboBox, ItemsSourceProperty);
        DataGridHelper.SyncColumnProperty(this, comboBox, ItemTemplateProperty);
        DataGridHelper.SyncColumnProperty(this, comboBox, SelectedValueBindingProperty);
        DataGridHelper.SyncColumnProperty(this, comboBox, SelectionBoxItemTemplateProperty);

        //if binding using SelectedItem then the DataGridBoundColumn handles that, otherwise we need to
        if (BindingTarget == SelectingItemsControl.SelectedValueProperty)
            comboBox.Bind(SelectingItemsControl.SelectedValueProperty, SelectedValue);
    }

    public override bool IsReadOnly
    {
        get
        {
            if (OwningGrid == null)
                return base.IsReadOnly;
            if (OwningGrid.IsReadOnly)
                return true;

            var valueBinding = Binding ?? SelectedValue;
            string path = (valueBinding as Binding)?.Path
                        ?? (valueBinding as CompiledBindingExtension)?.Path.ToString();
            return OwningGrid.DataConnection.GetPropertyIsReadOnly(path, out _);
        }
        set
        {
            base.IsReadOnly = value;
        }
    }
}

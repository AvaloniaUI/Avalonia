using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.ComponentModel;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Data;
using Avalonia.Controls.Utils;
using Avalonia.Data.Core;
using System.Collections;
using System.Linq;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a <see cref="DataGrid"/> column that hosts textual content in its cells. In edit mode data can be changed to a value from a collection hosted in a ComboBox.
    /// </summary>
    public class DataGridComboBoxColumn : DataGridBoundColumn
    {
        private const string DATAGRID_TextColumnCellTextBlockMarginKey = "DataGridTextColumnCellTextBlockMargin";

        private double? _fontSize;
        private DataGrid _owningGrid;
        private FontStyle? _fontStyle;
        private FontWeight? _fontWeight;
        private Brush _foreground;
        private IEnumerable items;

        public DataGridComboBoxColumn()
        {
            BindingTarget = ComboBox.SelectedItemProperty;
        }

        /// <summary>
        /// Identifies the ItemsSource dependency property.
        /// </summary>
        public static readonly DirectProperty<DataGridComboBoxColumn, IEnumerable> ItemsProperty =
            ItemsControl.ItemsProperty.AddOwner<DataGridComboBoxColumn>(o => o.Items, (o, v) => o.Items = v);

        /// <summary>
        /// Gets or sets a collection that is used to generate the content of the ComboBox while in editing mode.
        /// </summary>
        public IEnumerable Items
        {
            get => items;
            set => SetAndRaise(ItemsProperty, ref items, value);
        }

        /// <summary>
        /// Identifies the FontFamily dependency property.
        /// </summary>
        public static readonly AttachedProperty<FontFamily> FontFamilyProperty =
            TextBlock.FontFamilyProperty.AddOwner<DataGridTextColumn>();

        /// <summary>
        /// Gets or sets the font name.
        /// </summary>
        public FontFamily FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Identifies the FontSize dependency property.
        /// </summary>
        public static readonly AttachedProperty<double> FontSizeProperty =
            TextBlock.FontSizeProperty.AddOwner<DataGridTextColumn>();

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        // Use DefaultValue here so undo in the Designer will set this to NaN
        [DefaultValue(double.NaN)]
        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Identifies the FontStyle dependency property.
        /// </summary>
        public static readonly AttachedProperty<FontStyle> FontStyleProperty =
            TextBlock.FontStyleProperty.AddOwner<DataGridTextColumn>();

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        public FontStyle FontStyle
        {
            get => GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// Identifies the FontWeight dependency property.
        /// </summary>
        public static readonly AttachedProperty<FontWeight> FontWeightProperty =
            TextBlock.FontWeightProperty.AddOwner<DataGridTextColumn>();

        /// <summary>
        /// Gets or sets the font weight or thickness.
        /// </summary>
        public FontWeight FontWeight
        {
            get => GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// Identifies the Foreground dependency property.
        /// </summary>
        public static readonly AttachedProperty<IBrush> ForegroundProperty =
            TextBlock.ForegroundProperty.AddOwner<DataGridTextColumn>();

        /// <summary>
        /// Gets or sets a brush that describes the foreground of the column cells.
        /// </summary>
        public IBrush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == FontFamilyProperty
                || change.Property == FontSizeProperty
                || change.Property == FontStyleProperty
                || change.Property == FontWeightProperty
                || change.Property == ForegroundProperty
                || change.Property == ItemsProperty)
            {
                NotifyPropertyChanged(change.Property.Name);
            }
        }

        /// <summary>
        /// Gets a <see cref="T:Windows.UI.Xaml.Controls.ComboBox"/> control that is bound to the column's ItemsSource collection.
        /// </summary>
        /// <param name="cell">The cell that will contain the generated element.</param>
        /// <param name="dataItem">The data item represented by the row that contains the intended cell.</param>
        /// <returns>A new <see cref="T:Windows.UI.Xaml.Controls.ComboBox"/> control that is bound to the column's ItemsSource collection.</returns>
        protected override IControl GenerateEditingElementDirect(DataGridCell cell, object dataItem)
        {
            EnsureColumnBinding(dataItem);

            var comboBox = new ComboBox
            {
                Margin = new Thickness(),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                DataContext = dataItem
            };

            SyncProperties(comboBox);
            
            return comboBox;
        }

        /// <summary>
        /// Gets a read-only <see cref="TextBlock"/> element that is bound to the column's <see cref="DataGridBoundColumn.Binding"/> property value.
        /// </summary>
        /// <param name="cell">The cell that will contain the generated element.</param>
        /// <param name="dataItem">The data item represented by the row that contains the intended cell.</param>
        /// <returns>A new, read-only <see cref="TextBlock"/> element that is bound to the column's <see cref="DataGridBoundColumn.Binding"/> property value.</returns>
        protected override IControl GenerateElement(DataGridCell cell, object dataItem)
        {
            EnsureColumnBinding(dataItem);

            var textBlockElement = new TextBlock
            {
                [!Layoutable.MarginProperty] = new DynamicResourceExtension(DATAGRID_TextColumnCellTextBlockMarginKey),
                VerticalAlignment = VerticalAlignment.Center,
                [!TextBlock.TextProperty] = Binding
            };

            SyncProperties(textBlockElement);

            if (Binding != null)
            {
                textBlockElement.Text = dataItem.ToString();
            }

            return textBlockElement;
        }

        /// <summary>
        /// Causes the column cell being edited to revert to the specified value.
        /// </summary>
        /// <param name="editingElement">The element that the column displays for a cell in editing mode.</param>
        /// <param name="uneditedValue">The previous, unedited value in the cell being edited.</param>
        protected override void CancelCellEdit(IControl editingElement, object uneditedValue)
        {
            if (editingElement is ComboBox comboBox)
            {
                if (uneditedValue != null)
                {
                    var property = uneditedValue.GetType().GetNestedProperty(Binding.GetBindingPropertyName());

                    if (property == null)
                    {
                        comboBox.SelectedItem = uneditedValue;
                    }
                    else
                    {
                        var value = TypeHelper.GetNestedPropertyValue(uneditedValue, Binding.GetBindingPropertyName());
                        var selection = Items?.Cast<object>().FirstOrDefault(x => TypeHelper.GetNestedPropertyValue(x, Binding.GetBindingPropertyName()).Equals(value));

                        comboBox.SelectedItem = selection;
                    }
                }
                else
                {
                    comboBox.SelectedItem = null;
                }
            }
        }

        /// <summary>
        /// Called when the cell in the column enters editing mode.
        /// </summary>
        /// <param name="editingElement">The element that the column displays for a cell in editing mode.</param>
        /// <param name="editingEventArgs">Information about the user gesture that is causing a cell to enter editing mode.</param>
        /// <returns>The unedited value. </returns>
        protected override object PrepareCellForEdit(IControl editingElement, RoutedEventArgs editingEventArgs)
        {
            return (editingElement as ComboBox)?.SelectedItem;
        }

        /// <summary>
        /// Called by the DataGrid control when this column asks for its elements to be updated, because a property changed.
        /// </summary>
        protected internal override void RefreshCellContent(IControl element, string propertyName)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            if (element is AvaloniaObject content)
            {
                if (propertyName == nameof(FontFamily))
                {
                    DataGridHelper.SyncColumnProperty(this, content, FontFamilyProperty);
                }
                else if (propertyName == nameof(FontSize))
                {
                    DataGridHelper.SyncColumnProperty(this, content, FontSizeProperty);
                }
                else if (propertyName == nameof(FontStyle))
                {
                    DataGridHelper.SyncColumnProperty(this, content, FontStyleProperty);
                }
                else if (propertyName == nameof(FontWeight))
                {
                    DataGridHelper.SyncColumnProperty(this, content, FontWeightProperty);
                }
                else if (propertyName == nameof(Foreground))
                {
                    DataGridHelper.SyncColumnProperty(this, content, ForegroundProperty);
                }
                if (propertyName == nameof(Items)
                    && content is ComboBox)
                {
                    DataGridHelper.SyncColumnProperty(this, content, ItemsProperty);
                }
            }
            else
            {
                throw DataGridError.DataGrid.ValueIsNotAnInstanceOf("element", typeof(AvaloniaObject));
            }
        }

        private void SyncProperties(AvaloniaObject content)
        {
            DataGridHelper.SyncColumnProperty(this, content, FontFamilyProperty);
            DataGridHelper.SyncColumnProperty(this, content, FontSizeProperty);
            DataGridHelper.SyncColumnProperty(this, content, FontStyleProperty);
            DataGridHelper.SyncColumnProperty(this, content, FontWeightProperty);
            DataGridHelper.SyncColumnProperty(this, content, ForegroundProperty);
            if (content is ComboBox)
            {
                DataGridHelper.SyncColumnProperty(this, content, ItemsProperty);
            }
        }

        private void EnsureColumnBinding(object dataItem)
        {
            var path = (Binding as Binding)?.Path ?? (Binding as CompiledBindingExtension)?.Path.ToString();
            if (path == null)
            {
                if (!string.IsNullOrEmpty(Header as string))
                {
                    throw DataGridError.DataGridComboBoxColumn.UnsetBinding(Header as string);
                }

                throw DataGridError.DataGridComboBoxColumn.UnsetBinding(GetType());
            }

            var property = dataItem?.GetType().GetNestedProperty(path);

            if (property == null && dataItem != null)
            {
                throw DataGridError.DataGridComboBoxColumn.UnknownBindingPath(Binding, dataItem?.GetType());
            }
        }
    }
}

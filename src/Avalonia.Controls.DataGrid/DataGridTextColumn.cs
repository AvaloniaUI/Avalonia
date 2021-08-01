﻿// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.ComponentModel;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a <see cref="T:Avalonia.Controls.DataGrid" /> column that hosts textual content in its cells.
    /// </summary>
    public class DataGridTextColumn : DataGridBoundColumn
    {
        private const string DATAGRID_TextColumnCellTextBlockMarginKey = "DataGridTextColumnCellTextBlockMargin";

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Avalonia.Controls.DataGridTextColumn" /> class.
        /// </summary>
        public DataGridTextColumn()
        {
            BindingTarget = TextBox.TextProperty;
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
                || change.Property == ForegroundProperty)
            {
                NotifyPropertyChanged(change.Property.Name);
            }
        }

        /// <summary>
        /// Causes the column cell being edited to revert to the specified value.
        /// </summary>
        /// <param name="editingElement">The element that the column displays for a cell in editing mode.</param>
        /// <param name="uneditedValue">The previous, unedited value in the cell being edited.</param>
        protected override void CancelCellEdit(IControl editingElement, object uneditedValue)
        {
            if (editingElement is TextBox textBox)
            {
                string uneditedString = uneditedValue as string;
                textBox.Text = uneditedString ?? string.Empty;
            }
        }

        /// <summary>
        /// Gets a <see cref="T:Avalonia.Controls.TextBox" /> control that is bound to the column's <see cref="P:Avalonia.Controls.DataGridBoundColumn.Binding" /> property value.
        /// </summary>
        /// <param name="cell">The cell that will contain the generated element.</param>
        /// <param name="dataItem">The data item represented by the row that contains the intended cell.</param>
        /// <returns>A new <see cref="T:Avalonia.Controls.TextBox" /> control that is bound to the column's <see cref="P:Avalonia.Controls.DataGridBoundColumn.Binding" /> property value.</returns>
        protected override IControl GenerateEditingElementDirect(DataGridCell cell, object dataItem)
        {
            var textBox = new TextBox
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(Colors.Transparent)
            };

            SyncProperties(textBox);

            return textBox;
        }

        /// <summary>
        /// Gets a read-only <see cref="T:Avalonia.Controls.TextBlock" /> element that is bound to the column's <see cref="P:Avalonia.Controls.DataGridBoundColumn.Binding" /> property value.
        /// </summary>
        /// <param name="cell">The cell that will contain the generated element.</param>
        /// <param name="dataItem">The data item represented by the row that contains the intended cell.</param>
        /// <returns>A new, read-only <see cref="T:Avalonia.Controls.TextBlock" /> element that is bound to the column's <see cref="P:Avalonia.Controls.DataGridBoundColumn.Binding" /> property value.</returns>
        protected override IControl GenerateElement(DataGridCell cell, object dataItem)
        {
            TextBlock textBlockElement = new TextBlock
            {
                [!Layoutable.MarginProperty] = new DynamicResourceExtension(DATAGRID_TextColumnCellTextBlockMarginKey),
                VerticalAlignment = VerticalAlignment.Center
            };

            SyncProperties(textBlockElement);

            if (Binding != null)
            {
                textBlockElement.Bind(TextBlock.TextProperty, Binding);
            }
            return textBlockElement;
        }

        /// <summary>
        /// Called when the cell in the column enters editing mode.
        /// </summary>
        /// <param name="editingElement">The element that the column displays for a cell in editing mode.</param>
        /// <param name="editingEventArgs">Information about the user gesture that is causing a cell to enter editing mode.</param>
        /// <returns>The unedited value. </returns>
        protected override object PrepareCellForEdit(IControl editingElement, RoutedEventArgs editingEventArgs)
        {
            if (editingElement is TextBox textBox)
            {
                string uneditedText = textBox.Text ?? String.Empty;
                int len = uneditedText.Length;
                if (editingEventArgs is KeyEventArgs keyEventArgs && keyEventArgs.Key == Key.F2)
                {
                    // Put caret at the end of the text
                    textBox.SelectionStart = len;
                    textBox.SelectionEnd = len;
                }
                else
                {
                    // Select all text
                    textBox.SelectionStart = 0;
                    textBox.SelectionEnd = len;
                    textBox.CaretIndex = len;
                }

                return uneditedText;
            }
            return string.Empty;
        }

        /// <summary>
        /// Called by the DataGrid control when this column asks for its elements to be
        /// updated, because a property changed.
        /// </summary>
        protected internal override void RefreshCellContent(IControl element, string propertyName)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
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
        }
    }
}

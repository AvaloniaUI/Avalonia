// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.ComponentModel;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents a <see cref="T:Avalonia.Controls.DataGrid" /> column that hosts textual content in its cells.
    /// </summary>
    public class DataGridTextColumn : DataGridBoundColumn
    {

        private double? _fontSize;
        private FontStyle? _fontStyle;
        private FontWeight? _fontWeight;
        private IBrush _foreground;

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
        public static readonly StyledProperty<string> FontFamilyProperty =
            AvaloniaProperty.Register<DataGridTextColumn, string>(nameof(FontFamily));

        /// <summary>
        /// Gets or sets the font name.
        /// </summary>
        public string FontFamily
        {
            get { return GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        // Use DefaultValue here so undo in the Designer will set this to NaN
        [DefaultValue(double.NaN)]
        public double FontSize
        {
            get
            {
                return _fontSize ?? Double.NaN;
            }
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    NotifyPropertyChanged(nameof(FontSize));
                }
            }
        }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        public FontStyle FontStyle
        {
            get
            {
                return _fontStyle ?? FontStyle.Normal;
            }
            set
            {
                if (_fontStyle != value)
                {
                    _fontStyle = value;
                    NotifyPropertyChanged(nameof(FontStyle));
                }
            }
        }

        /// <summary>
        /// Gets or sets the font weight or thickness.
        /// </summary>
        public FontWeight FontWeight
        {
            get
            {
                return _fontWeight ?? FontWeight.Normal;
            }
            set
            {
                if (_fontWeight != value)
                {
                    _fontWeight = value;
                    NotifyPropertyChanged(nameof(FontWeight));
                }
            }
        }

        /// <summary>
        /// Gets or sets a brush that describes the foreground of the column cells.
        /// </summary>
        public IBrush Foreground
        {
            get
            {
                return _foreground;
            }
            set
            {
                if (_foreground != value)
                {
                    _foreground = value;
                    NotifyPropertyChanged(nameof(Foreground));
                }
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

            if (IsSet(FontFamilyProperty))
            {
                textBox.FontFamily = FontFamily;
            }
            if (_fontSize.HasValue)
            {
                textBox.FontSize = _fontSize.Value;
            }
            if (_fontStyle.HasValue)
            {
                textBox.FontStyle = _fontStyle.Value;
            }
            if (_fontWeight.HasValue)
            {
                textBox.FontWeight = _fontWeight.Value;
            }
            if (_foreground != null)
            {
                textBox.Foreground = _foreground;
            }

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
                Margin = new Thickness(4),
                VerticalAlignment = VerticalAlignment.Center
            };

            if (IsSet(FontFamilyProperty))
            {
                textBlockElement.FontFamily = FontFamily;
            }
            if (_fontSize.HasValue)
            {
                textBlockElement.FontSize = _fontSize.Value;
            }
            if (_fontStyle.HasValue)
            {
                textBlockElement.FontStyle = _fontStyle.Value;
            }
            if (_fontWeight.HasValue)
            {
                textBlockElement.FontWeight = _fontWeight.Value;
            }
            if (_foreground != null)
            {
                textBlockElement.Foreground = _foreground;
            }
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

            if(element is TextBox textBox)
            {
                if (propertyName == nameof(FontFamily))
                {
                    textBox.FontFamily = FontFamily;
                }
                else if (propertyName == nameof(FontSize))
                {
                    SetTextFontSize(textBox, TextBox.FontSizeProperty);
                }
                else if (propertyName == nameof(FontStyle))
                {
                    textBox.FontStyle = FontStyle;
                }
                else if (propertyName == nameof(FontWeight))
                {
                    textBox.FontWeight = FontWeight;
                }
                else if (propertyName == nameof(Foreground))
                {
                    textBox.Foreground = Foreground;
                }
                else
                {
                    if (FontFamily != null)
                    {
                        textBox.FontFamily = FontFamily;
                    }
                    SetTextFontSize(textBox, TextBox.FontSizeProperty);
                    textBox.FontStyle = FontStyle;
                    textBox.FontWeight = FontWeight;
                    if (Foreground != null)
                    {
                        textBox.Foreground = Foreground;
                    }
                }

            }
            else if (element is TextBlock textBlock)
            {
                if (propertyName == nameof(FontFamily))
                {
                    textBlock.FontFamily = FontFamily;
                }
                else if (propertyName == nameof(FontSize))
                {
                    SetTextFontSize(textBlock, TextBlock.FontSizeProperty);
                }
                else if (propertyName == nameof(FontStyle))
                {
                    textBlock.FontStyle = FontStyle;
                }
                else if (propertyName == nameof(FontWeight))
                {
                    textBlock.FontWeight = FontWeight;
                }
                else if (propertyName == nameof(Foreground))
                {
                    textBlock.Foreground = Foreground;
                }
                else
                {
                    if (FontFamily != null)
                    {
                        textBlock.FontFamily = FontFamily;
                    }
                    SetTextFontSize(textBlock, TextBlock.FontSizeProperty);
                    textBlock.FontStyle = FontStyle;
                    textBlock.FontWeight = FontWeight;
                    if (Foreground != null)
                    {
                        textBlock.Foreground = Foreground;
                    }
                }
            }
            else
            {
                throw DataGridError.DataGrid.ValueIsNotAnInstanceOfEitherOr("element", typeof(TextBox), typeof(TextBlock));
            }
        }

        private void SetTextFontSize(AvaloniaObject textElement, AvaloniaProperty fontSizeProperty)
        {
            double newFontSize = FontSize;
            if (double.IsNaN(newFontSize))
            {
                textElement.ClearValue(fontSizeProperty);
            }
            else
            {
                textElement.SetValue(fontSizeProperty, newFontSize);
            }
        }

    }
}

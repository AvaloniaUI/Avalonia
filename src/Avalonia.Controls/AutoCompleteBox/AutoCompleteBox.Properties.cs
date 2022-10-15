// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see https://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace Avalonia.Controls
{
    public partial class AutoCompleteBox
    {
        public static readonly StyledProperty<string?> WatermarkProperty =
            TextBox.WatermarkProperty.AddOwner<AutoCompleteBox>();

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.MinimumPrefixLength" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.MinimumPrefixLength" />
        /// dependency property.</value>
        public static readonly StyledProperty<int> MinimumPrefixLengthProperty =
            AvaloniaProperty.Register<AutoCompleteBox, int>(
                nameof(MinimumPrefixLength), 1,
                validate: IsValidMinimumPrefixLength);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.MinimumPopulateDelay" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.MinimumPopulateDelay" />
        /// dependency property.</value>
        public static readonly StyledProperty<TimeSpan> MinimumPopulateDelayProperty =
            AvaloniaProperty.Register<AutoCompleteBox, TimeSpan>(
                nameof(MinimumPopulateDelay),
                TimeSpan.Zero,
                validate: IsValidMinimumPopulateDelay);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.MaxDropDownHeight" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.MaxDropDownHeight" />
        /// dependency property.</value>
        public static readonly StyledProperty<double> MaxDropDownHeightProperty =
            AvaloniaProperty.Register<AutoCompleteBox, double>(
                nameof(MaxDropDownHeight),
                double.PositiveInfinity,
                validate: IsValidMaxDropDownHeight);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.IsTextCompletionEnabled" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.IsTextCompletionEnabled" />
        /// dependency property.</value>
        public static readonly StyledProperty<bool> IsTextCompletionEnabledProperty =
            AvaloniaProperty.Register<AutoCompleteBox, bool>(nameof(IsTextCompletionEnabled));

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemTemplate" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemTemplate" />
        /// dependency property.</value>
        public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
            AvaloniaProperty.Register<AutoCompleteBox, IDataTemplate>(nameof(ItemTemplate));

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.IsDropDownOpen" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.IsDropDownOpen" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, bool> IsDropDownOpenProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, bool>(
                nameof(IsDropDownOpen),
                o => o.IsDropDownOpen,
                (o, v) => o.IsDropDownOpen = v);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.SelectedItem" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.SelectedItem" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, object?> SelectedItemProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, object?>(
                nameof(SelectedItem),
                o => o.SelectedItem,
                (o, v) => o.SelectedItem = v,
                defaultBindingMode: BindingMode.TwoWay,
                enableDataValidation: true);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.Text" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.Text" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, string?> TextProperty =
            TextBlock.TextProperty.AddOwnerWithDataValidation<AutoCompleteBox>(
                o => o.Text,
                (o, v) => o.Text = v,
                defaultBindingMode: BindingMode.TwoWay,
                enableDataValidation: true);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.SearchText" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.SearchText" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, string?> SearchTextProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, string?>(
                nameof(SearchText),
                o => o.SearchText,
                unsetValue: string.Empty);

        /// <summary>
        /// Gets the identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.FilterMode" />
        /// dependency property.
        /// </summary>
        public static readonly StyledProperty<AutoCompleteFilterMode> FilterModeProperty =
            AvaloniaProperty.Register<AutoCompleteBox, AutoCompleteFilterMode>(
                nameof(FilterMode),
                defaultValue: AutoCompleteFilterMode.StartsWith,
                validate: IsValidFilterMode);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemFilter" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemFilter" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, AutoCompleteFilterPredicate<object?>?> ItemFilterProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, AutoCompleteFilterPredicate<object?>?>(
                nameof(ItemFilter),
                o => o.ItemFilter,
                (o, v) => o.ItemFilter = v);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.TextFilter" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.TextFilter" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, AutoCompleteFilterPredicate<string?>?> TextFilterProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, AutoCompleteFilterPredicate<string?>?>(
                nameof(TextFilter),
                o => o.TextFilter,
                (o, v) => o.TextFilter = v,
                unsetValue: AutoCompleteSearch.GetFilter(AutoCompleteFilterMode.StartsWith));

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemSelector" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemSelector" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, AutoCompleteSelector<object>?> ItemSelectorProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, AutoCompleteSelector<object>?>(
                nameof(ItemSelector),
                o => o.ItemSelector,
                (o, v) => o.ItemSelector = v);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.TextSelector" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.TextSelector" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, AutoCompleteSelector<string?>?> TextSelectorProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, AutoCompleteSelector<string?>?>(
                nameof(TextSelector),
                o => o.TextSelector,
                (o, v) => o.TextSelector = v);

        /// <summary>
        /// Identifies the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// dependency property.
        /// </summary>
        /// <value>The identifier for the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// dependency property.</value>
        public static readonly DirectProperty<AutoCompleteBox, IEnumerable?> ItemsProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, IEnumerable?>(
                nameof(Items),
                o => o.Items,
                (o, v) => o.Items = v);

        public static readonly DirectProperty<AutoCompleteBox, Func<string?, CancellationToken, Task<IEnumerable<object>>>?> AsyncPopulatorProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, Func<string?, CancellationToken, Task<IEnumerable<object>>>?>(
                nameof(AsyncPopulator),
                o => o.AsyncPopulator,
                (o, v) => o.AsyncPopulator = v);

        /// <summary>
        /// Gets or sets the minimum number of characters required to be entered
        /// in the text box before the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> displays
        /// possible matches.
        /// matches.
        /// </summary>
        /// <value>
        /// The minimum number of characters to be entered in the text box
        /// before the <see cref="T:Avalonia.Controls.AutoCompleteBox" />
        /// displays possible matches. The default is 1.
        /// </value>
        /// <remarks>
        /// If you set MinimumPrefixLength to -1, the AutoCompleteBox will
        /// not provide possible matches. There is no maximum value, but
        /// setting MinimumPrefixLength to value that is too large will
        /// prevent the AutoCompleteBox from providing possible matches as well.
        /// </remarks>
        public int MinimumPrefixLength
        {
            get => GetValue(MinimumPrefixLengthProperty);
            set => SetValue(MinimumPrefixLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the first possible match
        /// found during the filtering process will be displayed automatically
        /// in the text box.
        /// </summary>
        /// <value>
        /// True if the first possible match found will be displayed
        /// automatically in the text box; otherwise, false. The default is
        /// false.
        /// </value>
        public bool IsTextCompletionEnabled
        {
            get => GetValue(IsTextCompletionEnabledProperty);
            set => SetValue(IsTextCompletionEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="T:Avalonia.DataTemplate" /> used
        /// to display each item in the drop-down portion of the control.
        /// </summary>
        /// <value>The <see cref="T:Avalonia.DataTemplate" /> used to
        /// display each item in the drop-down. The default is null.</value>
        /// <remarks>
        /// You use the ItemTemplate property to specify the visualization
        /// of the data objects in the drop-down portion of the AutoCompleteBox
        /// control. If your AutoCompleteBox is bound to a collection and you
        /// do not provide specific display instructions by using a
        /// DataTemplate, the resulting UI of each item is a string
        /// representation of each object in the underlying collection.
        /// </remarks>
        public IDataTemplate ItemTemplate
        {
            get => GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum delay, after text is typed
        /// in the text box before the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control
        /// populates the list of possible matches in the drop-down.
        /// </summary>
        /// <value>The minimum delay, after text is typed in
        /// the text box, but before the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> populates
        /// the list of possible matches in the drop-down. The default is 0.</value>
        public TimeSpan MinimumPopulateDelay
        {
            get => GetValue(MinimumPopulateDelayProperty);
            set => SetValue(MinimumPopulateDelayProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum height of the drop-down portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control.
        /// </summary>
        /// <value>The maximum height of the drop-down portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control.
        /// The default is <see cref="F:System.Double.PositiveInfinity" />.</value>
        /// <exception cref="T:System.ArgumentException">The specified value is less than 0.</exception>
        public double MaxDropDownHeight
        {
            get => GetValue(MaxDropDownHeightProperty);
            set => SetValue(MaxDropDownHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the drop-down portion of
        /// the control is open.
        /// </summary>
        /// <value>
        /// True if the drop-down is open; otherwise, false. The default is
        /// false.
        /// </value>
        public bool IsDropDownOpen
        {
            get => _isDropDownOpen;
            set => SetAndRaise(IsDropDownOpenProperty, ref  _isDropDownOpen, value);
        }

        /// <summary>
        /// Gets or sets the  <see cref="T:Avalonia.Data.Binding" /> that
        /// is used to get the values for display in the text portion of
        /// the <see cref="T:Avalonia.Controls.AutoCompleteBox" />
        /// control.
        /// </summary>
        /// <value>The <see cref="T:Avalonia.Data.IBinding" /> object used
        /// when binding to a collection property.</value>
        [AssignBinding]
        public IBinding? ValueMemberBinding
        {
            get => _valueBindingEvaluator?.ValueBinding;
            set
            {
                if (ValueMemberBinding != value)
                {
                    _valueBindingEvaluator = new BindingEvaluator<string>(value);
                    OnValueMemberBindingChanged(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected item in the drop-down.
        /// </summary>
        /// <value>The selected item in the drop-down.</value>
        /// <remarks>
        /// If the IsTextCompletionEnabled property is true and text typed by
        /// the user matches an item in the ItemsSource collection, which is
        /// then displayed in the text box, the SelectedItem property will be
        /// a null reference.
        /// </remarks>
        public object? SelectedItem
        {
            get => _selectedItem;
            set => SetAndRaise(SelectedItemProperty, ref _selectedItem, value);
        }

        /// <summary>
        /// Gets or sets the text in the text box portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control.
        /// </summary>
        /// <value>The text in the text box portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control.</value>
        public string? Text
        {
            get => _text;
            set => SetAndRaise(TextProperty, ref _text, value);
        }

        /// <summary>
        /// Gets the text that is used to filter items in the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// item collection.
        /// </summary>
        /// <value>The text that is used to filter items in the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// item collection.</value>
        /// <remarks>
        /// The SearchText value is typically the same as the
        /// Text property, but is set after the TextChanged event occurs
        /// and before the Populating event.
        /// </remarks>
        public string? SearchText
        {
            get => _searchText;
            private set
            {
                try
                {
                    _allowWrite = true;
                    SetAndRaise(SearchTextProperty, ref _searchText, value);
                }
                finally
                {
                    _allowWrite = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets how the text in the text box is used to filter items
        /// specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// property for display in the drop-down.
        /// </summary>
        /// <value>One of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteFilterMode" />
        /// values The default is
        /// <see cref="F:Avalonia.Controls.AutoCompleteFilterMode.StartsWith" />.</value>
        /// <exception cref="T:System.ArgumentException">The specified value is
        /// not a valid
        /// <see cref="T:Avalonia.Controls.AutoCompleteFilterMode" />.</exception>
        /// <remarks>
        /// Use the FilterMode property to specify how possible matches are
        /// filtered. For example, possible matches can be filtered in a
        /// predefined or custom way. The search mode is automatically set to
        /// Custom if you set the ItemFilter property.
        /// </remarks>
        public AutoCompleteFilterMode FilterMode
        {
            get => GetValue(FilterModeProperty);
            set => SetValue(FilterModeProperty, value);
        }

        public string? Watermark
        {
            get => GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        /// <summary>
        /// Gets or sets the custom method that uses user-entered text to filter
        /// the items specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// property for display in the drop-down.
        /// </summary>
        /// <value>The custom method that uses the user-entered text to filter
        /// the items specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// property. The default is null.</value>
        /// <remarks>
        /// The filter mode is automatically set to Custom if you set the
        /// ItemFilter property.
        /// </remarks>
        public AutoCompleteFilterPredicate<object?>? ItemFilter
        {
            get => _itemFilter;
            set => SetAndRaise(ItemFilterProperty, ref _itemFilter, value);
        }

        /// <summary>
        /// Gets or sets the custom method that uses the user-entered text to
        /// filter items specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// property in a text-based way for display in the drop-down.
        /// </summary>
        /// <value>The custom method that uses the user-entered text to filter
        /// items specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// property in a text-based way for display in the drop-down.</value>
        /// <remarks>
        /// The search mode is automatically set to Custom if you set the
        /// TextFilter property.
        /// </remarks>
        public AutoCompleteFilterPredicate<string?>? TextFilter
        {
            get => _textFilter;
            set => SetAndRaise(TextFilterProperty, ref _textFilter, value);
        }

        /// <summary>
        /// Gets or sets the custom method that combines the user-entered
        /// text and one of the items specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />.
        /// </summary>
        /// <value>
        /// The custom method that combines the user-entered
        /// text and one of the items specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />.
        /// </value>
        public AutoCompleteSelector<object>? ItemSelector
        {
            get => _itemSelector;
            set => SetAndRaise(ItemSelectorProperty, ref _itemSelector, value);
        }

        /// <summary>
        /// Gets or sets the custom method that combines the user-entered
        /// text and one of the items specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// in a text-based way.
        /// </summary>
        /// <value>
        /// The custom method that combines the user-entered
        /// text and one of the items specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ItemsSource" />
        /// in a text-based way.
        /// </value>
        public AutoCompleteSelector<string?>? TextSelector
        {
            get => _textSelector;
            set => SetAndRaise(TextSelectorProperty, ref _textSelector, value);
        }

        public Func<string?, CancellationToken, Task<IEnumerable<object>>>? AsyncPopulator
        {
            get => _asyncPopulator;
            set => SetAndRaise(AsyncPopulatorProperty, ref _asyncPopulator, value);
        }

        /// <summary>
        /// Gets or sets a collection that is used to generate the items for the
        /// drop-down portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control.
        /// </summary>
        /// <value>The collection that is used to generate the items of the
        /// drop-down portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control.</value>
        public IEnumerable? Items
        {
            get => _itemsEnumerable;
            set => SetAndRaise(ItemsProperty, ref _itemsEnumerable, value);
        }
    }
}

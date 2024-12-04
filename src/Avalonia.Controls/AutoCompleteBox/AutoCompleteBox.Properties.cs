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
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    public partial class AutoCompleteBox
    {
        /// <summary>
        /// Defines see <see cref="TextBox.CaretIndex"/> property.
        /// </summary>
        public static readonly StyledProperty<int> CaretIndexProperty =
            TextBox.CaretIndexProperty.AddOwner<AutoCompleteBox>(new(
                defaultValue: 0,
                defaultBindingMode:BindingMode.TwoWay));

        public static readonly StyledProperty<string?> WatermarkProperty =
            TextBox.WatermarkProperty.AddOwner<AutoCompleteBox>();

        /// <summary>
        /// Identifies the <see cref="MinimumPrefixLength" /> property.
        /// </summary>
        /// <value>The identifier for the <see cref="MinimumPrefixLength" /> property.</value>
        public static readonly StyledProperty<int> MinimumPrefixLengthProperty =
            AvaloniaProperty.Register<AutoCompleteBox, int>(
                nameof(MinimumPrefixLength), 1,
                validate: IsValidMinimumPrefixLength);

        /// <summary>
        /// Identifies the <see cref="MinimumPopulateDelay" /> property.
        /// </summary>
        /// <value>The identifier for the <see cref="MinimumPopulateDelay" /> property.</value>
        public static readonly StyledProperty<TimeSpan> MinimumPopulateDelayProperty =
            AvaloniaProperty.Register<AutoCompleteBox, TimeSpan>(
                nameof(MinimumPopulateDelay),
                TimeSpan.Zero,
                validate: IsValidMinimumPopulateDelay);

        /// <summary>
        /// Identifies the <see cref="MaxDropDownHeight" /> property.
        /// </summary>
        /// <value>The identifier for the <see cref="MaxDropDownHeight" /> property.</value>
        public static readonly StyledProperty<double> MaxDropDownHeightProperty =
            AvaloniaProperty.Register<AutoCompleteBox, double>(
                nameof(MaxDropDownHeight),
                double.PositiveInfinity,
                validate: IsValidMaxDropDownHeight);

        /// <summary>
        /// Identifies the <see cref="IsTextCompletionEnabled" /> property.
        /// </summary>
        /// <value>The identifier for the <see cref="IsTextCompletionEnabled" /> property.</value>
        public static readonly StyledProperty<bool> IsTextCompletionEnabledProperty =
            AvaloniaProperty.Register<AutoCompleteBox, bool>(
                nameof(IsTextCompletionEnabled));

        /// <summary>
        /// Identifies the <see cref="ItemTemplate" /> property.
        /// </summary>
        /// <value>The identifier for the <see cref="ItemTemplate" /> property.</value>
        public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
            AvaloniaProperty.Register<AutoCompleteBox, IDataTemplate>(
                nameof(ItemTemplate));

        /// <summary>
        /// Identifies the <see cref="IsDropDownOpen" /> property.
        /// </summary>
        /// <value>The identifier for the <see cref="IsDropDownOpen" /> property.</value>
        public static readonly StyledProperty<bool> IsDropDownOpenProperty =
            AvaloniaProperty.Register<AutoCompleteBox, bool>(
                nameof(IsDropDownOpen));

        /// <summary>
        /// Identifies the <see cref="SelectedItem" /> property.
        /// </summary>
        /// <value>The identifier the <see cref="SelectedItem" /> property.</value>
        public static readonly StyledProperty<object?> SelectedItemProperty =
            AvaloniaProperty.Register<AutoCompleteBox, object?>(
                nameof(SelectedItem),
                defaultBindingMode: BindingMode.TwoWay,
                enableDataValidation: true);

        /// <summary>
        /// Identifies the <see cref="Text" /> property.
        /// </summary>
        /// <value>The identifier for the <see cref="Text" /> property.</value>
        public static readonly StyledProperty<string?> TextProperty =
            TextBlock.TextProperty.AddOwner<AutoCompleteBox>(new(string.Empty,
                defaultBindingMode: BindingMode.TwoWay,
                enableDataValidation: true));

        /// <summary>
        /// Identifies the <see cref="SearchText" /> property.
        /// </summary>
        /// <value>The identifier for the <see cref="SearchText" /> property.</value>
        public static readonly DirectProperty<AutoCompleteBox, string?> SearchTextProperty =
            AvaloniaProperty.RegisterDirect<AutoCompleteBox, string?>(
                nameof(SearchText),
                o => o.SearchText,
                unsetValue: string.Empty);

        /// <summary>
        /// Gets the identifier for the <see cref="FilterMode" /> property.
        /// </summary>
        public static readonly StyledProperty<AutoCompleteFilterMode> FilterModeProperty =
            AvaloniaProperty.Register<AutoCompleteBox, AutoCompleteFilterMode>(
                nameof(FilterMode),
                defaultValue: AutoCompleteFilterMode.StartsWith,
                validate: IsValidFilterMode);

        /// <summary>
        /// Identifies the <see cref="ItemFilter" /> property.
        /// </summary>
        /// <value>The identifier for the <see cref="ItemFilter" /> property.</value>
        public static readonly StyledProperty<AutoCompleteFilterPredicate<object?>?> ItemFilterProperty =
            AvaloniaProperty.Register<AutoCompleteBox, AutoCompleteFilterPredicate<object?>?>(
                nameof(ItemFilter));

        /// <summary>
        /// Identifies the <see cref="TextFilter" /> property.
        /// </summary>
        /// <value>The identifier for the <see cref="TextFilter" /> property.</value>
        public static readonly StyledProperty<AutoCompleteFilterPredicate<string?>?> TextFilterProperty =
            AvaloniaProperty.Register<AutoCompleteBox, AutoCompleteFilterPredicate<string?>?>(
                nameof(TextFilter),
                defaultValue: AutoCompleteSearch.GetFilter(AutoCompleteFilterMode.StartsWith));

        /// <summary>
        /// Identifies the <see cref="ItemSelector" /> property.
        /// </summary>
        /// <value>The identifier for the <see cref="ItemSelector" /> property.</value>
        public static readonly StyledProperty<AutoCompleteSelector<object>?> ItemSelectorProperty =
            AvaloniaProperty.Register<AutoCompleteBox, AutoCompleteSelector<object>?>(
                nameof(ItemSelector));

        /// <summary>
        /// Identifies the <see cref="TextSelector" /> property.
        /// </summary>
        /// <value>The identifier for the <see cref="TextSelector" /> property.</value>
        public static readonly StyledProperty<AutoCompleteSelector<string?>?> TextSelectorProperty =
            AvaloniaProperty.Register<AutoCompleteBox, AutoCompleteSelector<string?>?>(
                nameof(TextSelector));

        /// <summary>
        /// Identifies the <see cref="ItemsSource" /> property.
        /// </summary>
        /// <value>The identifier for the <see cref="ItemsSource" /> property.</value>
        public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
            AvaloniaProperty.Register<AutoCompleteBox, IEnumerable?>(
                nameof(ItemsSource));

        /// <summary>
        /// Identifies the <see cref="AsyncPopulator" /> property.
        /// </summary>
        /// <value>The identifier for the <see cref="AsyncPopulator" /> property.</value>
        public static readonly StyledProperty<Func<string?, CancellationToken, Task<IEnumerable<object>>>?> AsyncPopulatorProperty =
            AvaloniaProperty.Register<AutoCompleteBox, Func<string?, CancellationToken, Task<IEnumerable<object>>>?>(
                nameof(AsyncPopulator));

        /// <summary>
        /// Defines the <see cref="MaxLength"/> property
        /// </summary>
        public static readonly StyledProperty<int> MaxLengthProperty =
            TextBox.MaxLengthProperty.AddOwner<AutoCompleteBox>();

        /// <summary>
        /// Defines the <see cref="InnerLeftContent"/> property
        /// </summary>
        public static readonly StyledProperty<object?> InnerLeftContentProperty =
            TextBox.InnerLeftContentProperty.AddOwner<AutoCompleteBox>();

        /// <summary>
        /// Defines the <see cref="InnerRightContent"/> property
        /// </summary>
        public static readonly StyledProperty<object?> InnerRightContentProperty =
            TextBox.InnerRightContentProperty.AddOwner<AutoCompleteBox>();

        /// <summary>
        /// Gets or sets the caret index
        /// </summary>
        public int CaretIndex
        {
            get => GetValue(CaretIndexProperty);
            set => SetValue(CaretIndexProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum number of characters required to be entered
        /// in the text box before the <see cref="AutoCompleteBox" /> displays possible matches.
        /// </summary>
        /// <value>
        /// The minimum number of characters to be entered in the text box
        /// before the <see cref="AutoCompleteBox" />
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
        /// <see cref="AutoCompleteBox" /> control
        /// populates the list of possible matches in the drop-down.
        /// </summary>
        /// <value>The minimum delay, after text is typed in
        /// the text box, but before the
        /// <see cref="AutoCompleteBox" /> populates
        /// the list of possible matches in the drop-down. The default is 0.</value>
        public TimeSpan MinimumPopulateDelay
        {
            get => GetValue(MinimumPopulateDelayProperty);
            set => SetValue(MinimumPopulateDelayProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum height of the drop-down portion of the
        /// <see cref="AutoCompleteBox" /> control.
        /// </summary>
        /// <value>The maximum height of the drop-down portion of the
        /// <see cref="AutoCompleteBox" /> control.
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
            get => GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        /// <summary>
        /// Gets or sets the  <see cref="T:Avalonia.Data.Binding" /> that
        /// is used to get the values for display in the text portion of
        /// the <see cref="AutoCompleteBox" />
        /// control.
        /// </summary>
        /// <value>The <see cref="T:Avalonia.Data.IBinding" /> object used
        /// when binding to a collection property.</value>
        [AssignBinding]
        [InheritDataTypeFromItems(nameof(ItemsSource))]
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
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        /// <summary>
        /// Gets or sets the text in the text box portion of the
        /// <see cref="AutoCompleteBox" /> control.
        /// </summary>
        /// <value>The text in the text box portion of the
        /// <see cref="AutoCompleteBox" /> control.</value>
        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Gets the text that is used to filter items in the
        /// <see cref="ItemsSource" /> item collection.
        /// </summary>
        /// <value>The text that is used to filter items in the
        /// <see cref="ItemsSource" /> item collection.</value>
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
        /// specified by the <see cref="ItemsSource" />
        /// property for display in the drop-down.
        /// </summary>
        /// <value>One of the <see cref="AutoCompleteFilterMode" />
        /// values The default is <see cref="AutoCompleteFilterMode.StartsWith" />.</value>
        /// <exception cref="T:System.ArgumentException">The specified value is not a valid
        /// <see cref="AutoCompleteFilterMode" />.</exception>
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
        /// the items specified by the <see cref="ItemsSource" />
        /// property for display in the drop-down.
        /// </summary>
        /// <value>The custom method that uses the user-entered text to filter
        /// the items specified by the <see cref="ItemsSource" />
        /// property. The default is null.</value>
        /// <remarks>
        /// The filter mode is automatically set to Custom if you set the
        /// ItemFilter property.
        /// </remarks>
        public AutoCompleteFilterPredicate<object?>? ItemFilter
        {
            get => GetValue(ItemFilterProperty);
            set => SetValue(ItemFilterProperty, value);
        }

        /// <summary>
        /// Gets or sets the custom method that uses the user-entered text to
        /// filter items specified by the <see cref="ItemsSource" />
        /// property in a text-based way for display in the drop-down.
        /// </summary>
        /// <value>The custom method that uses the user-entered text to filter
        /// items specified by the <see cref="ItemsSource" />
        /// property in a text-based way for display in the drop-down.</value>
        /// <remarks>
        /// The search mode is automatically set to Custom if you set the
        /// TextFilter property.
        /// </remarks>
        public AutoCompleteFilterPredicate<string?>? TextFilter
        {
            get => GetValue(TextFilterProperty);
            set => SetValue(TextFilterProperty, value);
        }

        /// <summary>
        /// Gets or sets the custom method that combines the user-entered
        /// text and one of the items specified by the <see cref="ItemsSource" />.
        /// </summary>
        /// <value>
        /// The custom method that combines the user-entered
        /// text and one of the items specified by the <see cref="ItemsSource" />.
        /// </value>
        public AutoCompleteSelector<object>? ItemSelector
        {
            get => GetValue(ItemSelectorProperty);
            set => SetValue(ItemSelectorProperty, value);
        }

        /// <summary>
        /// Gets or sets the custom method that combines the user-entered
        /// text and one of the items specified by the
        /// <see cref="ItemsSource" /> in a text-based way.
        /// </summary>
        /// <value>
        /// The custom method that combines the user-entered
        /// text and one of the items specified by the <see cref="ItemsSource" />
        /// in a text-based way.
        /// </value>
        public AutoCompleteSelector<string?>? TextSelector
        {
            get => GetValue(TextSelectorProperty);
            set => SetValue(TextSelectorProperty, value);
        }

        public Func<string?, CancellationToken, Task<IEnumerable<object>>>? AsyncPopulator
        {
            get => GetValue(AsyncPopulatorProperty);
            set => SetValue(AsyncPopulatorProperty, value);
        }

        /// <summary>
        /// Gets or sets a collection that is used to generate the items for the
        /// drop-down portion of the <see cref="AutoCompleteBox" /> control.
        /// </summary>
        /// <value>The collection that is used to generate the items of the
        /// drop-down portion of the <see cref="AutoCompleteBox" /> control.</value>
        public IEnumerable? ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }
       
        /// <summary>
        /// Gets or sets the maximum number of characters that the <see cref="AutoCompleteBox"/> can accept.
        /// This constraint only applies for manually entered (user-inputted) text.
        /// </summary>
        public int MaxLength
        {
            get => GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }
      
        /// <summary>
        /// Gets or sets custom content that is positioned on the left side of the text layout box
        /// </summary>
        public object? InnerLeftContent
        {
            get => GetValue(InnerLeftContentProperty);
            set => SetValue(InnerLeftContentProperty, value);
        }

        /// <summary>
        /// Gets or sets custom content that is positioned on the right side of the text layout box
        /// </summary>
        public object? InnerRightContent
        {
            get => GetValue(InnerRightContentProperty);
            set => SetValue(InnerRightContentProperty, value);
        }      
    }
}

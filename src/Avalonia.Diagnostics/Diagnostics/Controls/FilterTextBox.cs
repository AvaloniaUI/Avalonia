using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;

namespace Avalonia.Diagnostics.Controls
{
    internal class FilterTextBox : TextBox
    {
        public static readonly StyledProperty<bool> UseRegexFilterProperty =
            AvaloniaProperty.Register<FilterTextBox, bool>(nameof(UseRegexFilter),
                defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<bool> UseCaseSensitiveFilterProperty =
            AvaloniaProperty.Register<FilterTextBox, bool>(nameof(UseCaseSensitiveFilter),
                defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<bool> UseWholeWordFilterProperty =
            AvaloniaProperty.Register<FilterTextBox, bool>(nameof(UseWholeWordFilter),
                defaultBindingMode: BindingMode.TwoWay);

        public FilterTextBox()
        {
            Classes.Add("filter-text-box");
        }

        public bool UseRegexFilter
        {
            get => GetValue(UseRegexFilterProperty);
            set => SetValue(UseRegexFilterProperty, value);
        }

        public bool UseCaseSensitiveFilter
        {
            get => GetValue(UseCaseSensitiveFilterProperty);
            set => SetValue(UseCaseSensitiveFilterProperty,value);
        }

        public bool UseWholeWordFilter
        {
            get => GetValue(UseWholeWordFilterProperty);
            set => SetValue(UseWholeWordFilterProperty, value);
        }

        protected override Type StyleKeyOverride => typeof(TextBox);
    }
}

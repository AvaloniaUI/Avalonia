using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls.MaskedTextBox.Enums;
using Avalonia.Controls.MaskedTextBox.Filters;

namespace Avalonia.Controls.MaskedTextBox
{
    public class FilterProvider
    {
        private static readonly Lazy<FilterProvider> InstanceLazy = new(() => new FilterProvider());
        public static FilterProvider Instance => InstanceLazy.Value;

        private readonly Dictionary<FilterType, DefaultFilter> _predefinedFilters = new();

        private FilterProvider()
        {
            LoadValues();
        }

        private void LoadValues()
        {
            _predefinedFilters.Add(FilterType.Any, DefaultFilter.NullFilter);
            _predefinedFilters.Add(FilterType.Number, RegExFilter.NumberFilter);
            _predefinedFilters.Add(FilterType.Decimal, RegExFilter.DecimalFilter);
            _predefinedFilters.Add(FilterType.UNumber, RegExFilter.UNumberFilter);
            _predefinedFilters.Add(FilterType.UDecimal, RegExFilter.UDecimalFilter);
        }

        public DefaultFilter FilterForMaskedType(FilterType type)
        {
            var filter = _predefinedFilters[type];
            return filter ?? DefaultFilter.NullFilter;
        }
    }
}

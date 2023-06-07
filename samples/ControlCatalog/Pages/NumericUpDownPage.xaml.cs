using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using MiniMvvm;

namespace ControlCatalog.Pages
{
    public class NumericUpDownPage : UserControl
    {
        public NumericUpDownPage()
        {
            this.InitializeComponent();
            var viewModel = new NumbersPageViewModel();
            DataContext = viewModel;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static IValueConverter CultureConverter =
            new FuncValueConverter<CultureInfo, NumberFormatInfo>(c => (c ?? CultureInfo.CurrentCulture).NumberFormat);
    }

    public class NumbersPageViewModel : ViewModelBase
    {
        private IList<FormatObject>? _formats;
        private FormatObject? _selectedFormat;
        private IList<Location>? _spinnerLocations;

        private double _doubleValue;
        private decimal _decimalValue;

        public NumbersPageViewModel()
        {
            _selectedFormat = Formats.FirstOrDefault();
        }

        public double DoubleValue
        {
            get { return _doubleValue; }
            set { this.RaiseAndSetIfChanged(ref _doubleValue, value); }
        }

        public decimal DecimalValue
        {
            get { return _decimalValue; }
            set { this.RaiseAndSetIfChanged(ref _decimalValue, value); }
        }

        public IList<FormatObject> Formats
        {
            get
            {
                return _formats ?? (_formats = new List<FormatObject>()
                {
                    new FormatObject() {Name = "Currency", Value = "C2"},
                    new FormatObject() {Name = "Fixed point", Value = "F2"},
                    new FormatObject() {Name = "General", Value = "G"},
                    new FormatObject() {Name = "Number", Value = "N"},
                    new FormatObject() {Name = "Percent", Value = "P"},
                    new FormatObject() {Name = "Degrees", Value = "{0:N2} °"},
                });
            }
        }

        public IList<Location> SpinnerLocations
        {
            get
            {
                if (_spinnerLocations == null)
                {
                    _spinnerLocations = new List<Location>();
                    foreach (Location value in Enum.GetValues(typeof(Location)))
                    {
                        _spinnerLocations.Add(value);
                    }
                }
                return _spinnerLocations ;
            }
        }

        // Trimmed-mode friendly where we might not have cultures
        public IList<CultureInfo?> Cultures { get; } = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Where(c => new[] { "en-US", "en-GB", "fr-FR", "ar-DZ", "zh-CH", "cs-CZ" }.Contains(c.Name))
            .ToArray();

        public FormatObject? SelectedFormat
        {
            get { return _selectedFormat; }
            set { this.RaiseAndSetIfChanged(ref _selectedFormat, value); }
        }
    }

    public class FormatObject
    {
        public string? Value { get; set; }
        public string? Name { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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

    }

    public class NumbersPageViewModel : ViewModelBase
    {
        private IList<FormatObject> _formats;
        private FormatObject _selectedFormat;
        private IList<Location> _spinnerLocations;

        public NumbersPageViewModel()
        {
            SelectedFormat = Formats.FirstOrDefault();
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

        public IList<CultureInfo> Cultures { get; } = new List<CultureInfo>()
        {
            new CultureInfo("en-US"),
            new CultureInfo("en-GB"),
            new CultureInfo("fr-FR"),
            new CultureInfo("ar-DZ"),
            new CultureInfo("zh-CN"),
            new CultureInfo("cs-CZ")
        };

        public FormatObject SelectedFormat
        {
            get { return _selectedFormat; }
            set { this.RaiseAndSetIfChanged(ref _selectedFormat, value); }
        }
    }

    public class FormatObject
    {
        public string Value { get; set; }
        public string Name { get; set; }
    }
}

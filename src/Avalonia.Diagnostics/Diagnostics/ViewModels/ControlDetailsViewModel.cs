using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class ControlDetailsViewModel : ViewModelBase, IDisposable
    {
        private readonly IVisual _control;
        private readonly IDictionary<object, List<PropertyViewModel>> _propertyIndex;
        private AvaloniaPropertyViewModel _selectedProperty;
        private string _propertyFilter;

        public ControlDetailsViewModel(IVisual control, string propertyFilter)
        {
            _control = control;

            var properties = GetAvaloniaProperties(control)
                .Concat(GetClrProperties(control))
                .OrderBy(x => x, PropertyComparer.Instance)
                .ThenBy(x => x.Name)
                .ToList();

            _propertyIndex = properties.GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.ToList());
            _propertyFilter = propertyFilter;

            var view = new DataGridCollectionView(properties);
            view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(AvaloniaPropertyViewModel.Group)));
            view.Filter = FilterProperty;
            PropertiesView = view;

            if (control is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged += ControlPropertyChanged;
            }

            if (control is AvaloniaObject ao)
            {
                ao.PropertyChanged += ControlPropertyChanged;
            }
        }

        public DataGridCollectionView PropertiesView { get; }

        public string PropertyFilter
        {
            get => _propertyFilter;
            set
            {
                if (RaiseAndSetIfChanged(ref _propertyFilter, value))
                {
                    PropertiesView.Refresh();
                }
            }
        }

        public AvaloniaPropertyViewModel SelectedProperty
        {
            get => _selectedProperty;
            set => RaiseAndSetIfChanged(ref _selectedProperty, value);
        }

        public void Dispose()
        {
            if (_control is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged -= ControlPropertyChanged;
            }

            if (_control is AvaloniaObject ao)
            {
                ao.PropertyChanged -= ControlPropertyChanged;
            }
        }

        private IEnumerable<PropertyViewModel> GetAvaloniaProperties(object o)
        {
            if (o is AvaloniaObject ao)
            {
                return AvaloniaPropertyRegistry.Instance.GetRegistered(ao)
                    .Union(AvaloniaPropertyRegistry.Instance.GetRegisteredAttached(ao.GetType()))
                    .Select(x => new AvaloniaPropertyViewModel(ao, x));
            }
            else
            {
                return Enumerable.Empty<AvaloniaPropertyViewModel>();
            }
        }

        private IEnumerable<PropertyViewModel> GetClrProperties(object o)
        {
            foreach (var p in GetClrProperties(o, o.GetType()))
            {
                yield return p;
            }

            foreach (var i in o.GetType().GetInterfaces())
            {
                foreach (var p in GetClrProperties(o, i))
                {
                    yield return p;
                }
            }
        }

        private IEnumerable<PropertyViewModel> GetClrProperties(object o, Type t)
        {
            return t.GetProperties()
                .Where(x => x.GetIndexParameters().Length == 0)
                .Select(x => new ClrPropertyViewModel(o, x));
        }

        private void ControlPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (_propertyIndex.TryGetValue(e.Property, out var properties))
            {
                foreach (var property in properties)
                {
                    property.Update();
                }
            }
        }

        private void ControlPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_propertyIndex.TryGetValue(e.PropertyName, out var properties))
            {
                foreach (var property in properties)
                {
                    property.Update();
                }
            }
        }

        private bool FilterProperty(object arg)
        {
            if (!string.IsNullOrWhiteSpace(PropertyFilter) && arg is PropertyViewModel property)
            {
                return property.Name.IndexOf(PropertyFilter, StringComparison.OrdinalIgnoreCase) != -1;
            }

            return true;
        }

        private class PropertyComparer : IComparer<PropertyViewModel>
        {
            public static PropertyComparer Instance { get; } = new PropertyComparer();

            public int Compare(PropertyViewModel x, PropertyViewModel y)
            {
                var groupX = GroupIndex(x.Group);
                var groupY = GroupIndex(y.Group);

                if (groupX != groupY)
                {
                    return groupX - groupY;
                }
                else
                {
                    return string.CompareOrdinal(x.Name, y.Name);
                }
            }

            private int GroupIndex(string group)
            {
                switch (group)
                {
                    case "Properties": return 0;
                    case "Attached Properties": return 1;
                    case "CLR Properties": return 2;
                    default: return 3;
                }
            }
        }
    }
}

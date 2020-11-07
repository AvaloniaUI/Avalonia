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

        public ControlDetailsViewModel(TreePageViewModel treePage, IVisual control)
        {
            _control = control;

            TreePage = treePage;

            var properties = GetAvaloniaProperties(control)
                .Concat(GetClrProperties(control))
                .OrderBy(x => x, PropertyComparer.Instance)
                .ThenBy(x => x.Name)
                .ToList();

            _propertyIndex = properties.GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.ToList());

            var view = new DataGridCollectionView(properties);
            view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(AvaloniaPropertyViewModel.Group)));
            view.Filter = FilterProperty;
            PropertiesView = view;

            Layout = new ControlLayoutViewModel(control);

            if (control is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged += ControlPropertyChanged;
            }

            if (control is AvaloniaObject ao)
            {
                ao.PropertyChanged += ControlPropertyChanged;
            }
        }

        public TreePageViewModel TreePage { get; }

        public DataGridCollectionView PropertiesView { get; }

        public AvaloniaPropertyViewModel SelectedProperty
        {
            get => _selectedProperty;
            set => RaiseAndSetIfChanged(ref _selectedProperty, value);
        }
        
        public ControlLayoutViewModel Layout { get; }

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

            Layout.ControlPropertyChanged(sender, e);
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
            if (!string.IsNullOrWhiteSpace(TreePage.PropertyFilter) && arg is PropertyViewModel property)
            {
                if (TreePage.UseRegexFilter)
                {
                    return TreePage.FilterRegex?.IsMatch(property.Name) ?? true;
                }

                return property.Name.IndexOf(TreePage.PropertyFilter, StringComparison.OrdinalIgnoreCase) != -1;
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

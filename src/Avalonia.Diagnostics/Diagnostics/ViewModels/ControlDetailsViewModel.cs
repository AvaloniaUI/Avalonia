using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class StyleViewModel : ViewModelBase
    {
        private readonly IStyleInstance _styleInstance;
        private bool _isActive;

        public StyleViewModel(IStyleInstance styleInstance, string name, List<SetterViewModel> setters)
        {
            _styleInstance = styleInstance;
            IsActive = styleInstance.IsActive;
            Name = name;
            Setters = setters;
        }

        public bool IsActive
        {
            get => _isActive;
            set => RaiseAndSetIfChanged(ref _isActive, value);
        }

        public string Name { get; }

        public List<SetterViewModel> Setters { get; }

        public void Update()
        {
            IsActive = _styleInstance.IsActive;
        }
    }

    internal enum ValueKind
    {
        Regular,
        Resource
    }

    internal class SetterViewModel : ViewModelBase
    {
        public string Name { get; }

        public object Value { get; }

        public ValueKind Kind { get; }

        public bool IsSpecialKind => Kind != ValueKind.Regular;

        public IBrush KindColor { get; }

        public SetterViewModel(string name, object value, ValueKind kind)
        {
            Name = name;
            Value = value;
            Kind = kind;

            if (Kind == ValueKind.Resource)
            {
                KindColor = Brushes.Brown;
            }
            else
            {
                KindColor = Brushes.Transparent;
            }
        }
    }

    internal class PseudoClassesViewModel : ViewModelBase
    {
        private readonly StyledElement _source;
        private readonly IPseudoClasses _pseudoClasses;
        private bool _isActive;
        private bool _isUpdating;

        public PseudoClassesViewModel(string name, StyledElement source)
        {
            Name = name;
            _source = source;
            _pseudoClasses = _source.Classes;

            Update();
        }

        public string Name { get; }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                RaiseAndSetIfChanged(ref _isActive, value);

                if (!_isUpdating)
                {
                    _pseudoClasses.Set(Name, value);
                }
            }
        }

        public void Update()
        {
            try
            {
                _isUpdating = true;

                IsActive = _source.Classes.Contains(Name);
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }

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

            AppliedStyles = new ObservableCollection<StyleViewModel>();
            PseudoClasses = new ObservableCollection<PseudoClassesViewModel>();

            if (control is StyledElement styledElement)
            {
                styledElement.Classes.CollectionChanged += OnClassesChanged;

                var pseudoClassAttributes = styledElement.GetType().GetCustomAttributes<PseudoClassesAttribute>(true);

                foreach (var classAttribute in pseudoClassAttributes)
                {
                    foreach (var className in classAttribute.PseudoClasses)
                    {
                        PseudoClasses.Add(new PseudoClassesViewModel(className, styledElement));
                    }
                }

                var styleDiagnostics = styledElement.GetStyleDiagnostics();

                foreach (var appliedStyle in styleDiagnostics.AppliedStyles)
                {
                    var styleSource = appliedStyle.Source;

                    var setters = new List<SetterViewModel>();

                    if (styleSource is Style style)
                    {
                        foreach (var setter in style.Setters)
                        {
                            if (setter is Setter regularSetter)
                            {
                                var setterValue = regularSetter.Value;

                                ValueKind kind = ValueKind.Regular;

                                if (setterValue is DynamicResourceExtension dynResource)
                                {
                                    var resolved = styledElement.FindResource(dynResource.ResourceKey);

                                    if (resolved != null)
                                    {
                                        setterValue = $"{resolved} ({dynResource.ResourceKey})";
                                    }
                                    else
                                    {
                                        setterValue = dynResource.ResourceKey;
                                    }

                                    kind = ValueKind.Resource;
                                }

                                setters.Add(new SetterViewModel(regularSetter.Property?.Name ?? "?", setterValue, kind));
                            }
                        }

                        AppliedStyles.Add(new StyleViewModel(appliedStyle, style.Selector?.ToString() ?? "No selector", setters));
                    }
                }
            }
        }

        public TreePageViewModel TreePage { get; }

        public DataGridCollectionView PropertiesView { get; }

        public ObservableCollection<StyleViewModel> AppliedStyles { get; }

        public ObservableCollection<PseudoClassesViewModel> PseudoClasses { get; }

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

            if (_control is StyledElement se)
            {
                se.Classes.CollectionChanged -= OnClassesChanged;
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

            UpdateStyles();
        }

        private void OnClassesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateStyles();
        }

        private void UpdateStyles()
        {
            foreach (var style in AppliedStyles)
            {
                style.Update();
            }

            foreach (var pseudoClass in PseudoClasses)
            {
                pseudoClass.Update();
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

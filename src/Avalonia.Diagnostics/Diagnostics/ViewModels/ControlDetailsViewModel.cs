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
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class ControlDetailsViewModel : ViewModelBase, IDisposable
    {
        private readonly IVisual _control;
        private IDictionary<object, List<PropertyViewModel>>? _propertyIndex;
        private PropertyViewModel? _selectedProperty;
        private DataGridCollectionView? _propertiesView;
        private bool _snapshotStyles;
        private bool _showInactiveStyles;
        private string? _styleStatus;
        private object? _selectedEntity;
        private readonly Stack<(string Name,object Entry)> _selectedEntitiesStack = new();
        private string? _selectedEntityName;
        private string? _selectedEntityType;

        public ControlDetailsViewModel(TreePageViewModel treePage, IVisual control)
        {
            _control = control;

            TreePage = treePage;
            
            Layout = new ControlLayoutViewModel(control);

            NavigateToProperty(control, (control as IControl)?.Name ?? control.ToString()); 

            AppliedStyles = new ObservableCollection<StyleViewModel>();
            PseudoClasses = new ObservableCollection<PseudoClassViewModel>();

            if (control is StyledElement styledElement)
            {
                styledElement.Classes.CollectionChanged += OnClassesChanged;

                var pseudoClassAttributes = styledElement.GetType().GetCustomAttributes<PseudoClassesAttribute>(true);

                foreach (var classAttribute in pseudoClassAttributes)
                {
                    foreach (var className in classAttribute.PseudoClasses)
                    {
                        PseudoClasses.Add(new PseudoClassViewModel(className, styledElement));
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
                            if (setter is Setter regularSetter
                                && regularSetter.Property != null)
                            {
                                var setterValue = regularSetter.Value;

                                var resourceInfo = GetResourceInfo(setterValue);

                                SetterViewModel setterVm;

                                if (resourceInfo.HasValue)
                                {
                                    var resourceKey = resourceInfo.Value.resourceKey;
                                    var resourceValue = styledElement.FindResource(resourceKey);

                                    setterVm = new ResourceSetterViewModel(regularSetter.Property, resourceKey, resourceValue, resourceInfo.Value.isDynamic);
                                }
                                else
                                {
                                    setterVm = new SetterViewModel(regularSetter.Property, setterValue);
                                }

                                setters.Add(setterVm);
                            }
                        }

                        AppliedStyles.Add(new StyleViewModel(appliedStyle, style.Selector?.ToString() ?? "No selector", setters));
                    }
                }

                UpdateStyles();
            }
        }

        private (object resourceKey, bool isDynamic)? GetResourceInfo(object? value)
        {
            if (value is StaticResourceExtension staticResource)
            {
                return (staticResource.ResourceKey, false);
            }
            else if (value is DynamicResourceExtension dynamicResource
                && dynamicResource.ResourceKey != null)
            {
                return (dynamicResource.ResourceKey, true);
            }

            return null;
        }

        public TreePageViewModel TreePage { get; }

        public DataGridCollectionView? PropertiesView
        {
            get => _propertiesView;
            private set => RaiseAndSetIfChanged(ref _propertiesView, value);
        }

        public ObservableCollection<StyleViewModel> AppliedStyles { get; }

        public ObservableCollection<PseudoClassViewModel> PseudoClasses { get; }

        public object? SelectedEntity
        {
            get => _selectedEntity;
            set
            {
                RaiseAndSetIfChanged(ref _selectedEntity, value);
               
            }
        }

        public string? SelectedEntityName
        {
            get => _selectedEntityName;
            set
            {
                RaiseAndSetIfChanged(ref _selectedEntityName, value);
               
            }
        }
        
        public string? SelectedEntityType
        {
            get => _selectedEntityType;
            set
            {
                RaiseAndSetIfChanged(ref _selectedEntityType, value);
               
            }
        }
        
        public PropertyViewModel? SelectedProperty
        {
            get => _selectedProperty;
            set => RaiseAndSetIfChanged(ref _selectedProperty, value);
        }

        public bool SnapshotStyles
        {
            get => _snapshotStyles;
            set => RaiseAndSetIfChanged(ref _snapshotStyles, value);
        }

        public bool ShowInactiveStyles
        {
            get => _showInactiveStyles;
            set => RaiseAndSetIfChanged(ref _showInactiveStyles, value);
        }

        public string? StyleStatus
        {
            get => _styleStatus;
            set => RaiseAndSetIfChanged(ref _styleStatus, value);
        }

        public ControlLayoutViewModel Layout { get; }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(SnapshotStyles))
            {
                if (!SnapshotStyles)
                {
                    UpdateStyles();
                }
            }
        }

        public void UpdateStyleFilters()
        {
            foreach (var style in AppliedStyles)
            {
                var hasVisibleSetter = false;

                foreach (var setter in style.Setters)
                {
                    setter.IsVisible = TreePage.SettersFilter.Filter(setter.Name);

                    hasVisibleSetter |= setter.IsVisible;
                }

                style.IsVisible = hasVisibleSetter;
            }
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

        private void ControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (_propertyIndex is { } && _propertyIndex.TryGetValue(e.Property, out var properties))
            {
                foreach (var property in properties)
                {
                    property.Update();
                }
            }

            Layout.ControlPropertyChanged(sender, e);
        }

        private void ControlPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != null
                && _propertyIndex is { }
                && _propertyIndex.TryGetValue(e.PropertyName, out var properties))
            {
                foreach (var property in properties)
                {
                    property.Update();
                }
            }

            if (!SnapshotStyles)
            {
                UpdateStyles();
            }
        }

        private void OnClassesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!SnapshotStyles)
            {
                UpdateStyles();
            }
        }

        private void UpdateStyles()
        {
            int activeCount = 0;

            foreach (var style in AppliedStyles)
            {
                style.Update();

                if (style.IsActive)
                {
                    activeCount++;
                }
            }

            var propertyBuckets = new Dictionary<AvaloniaProperty, List<SetterViewModel>>();

            foreach (var style in AppliedStyles)
            {
                if (!style.IsActive)
                {
                    continue;
                }

                foreach (var setter in style.Setters)
                {
                    if (propertyBuckets.TryGetValue(setter.Property, out var setters))
                    {
                        foreach (var otherSetter in setters)
                        {
                            otherSetter.IsActive = false;
                        }

                        setter.IsActive = true;

                        setters.Add(setter);
                    }
                    else
                    {
                        setter.IsActive = true;

                        setters = new List<SetterViewModel> { setter };

                        propertyBuckets.Add(setter.Property, setters);
                    }
                }
            }

            foreach (var pseudoClass in PseudoClasses)
            {
                pseudoClass.Update();
            }

            StyleStatus = $"Styles ({activeCount}/{AppliedStyles.Count} active)";
        }

        private bool FilterProperty(object arg)
        {
            return !(arg is PropertyViewModel property) || TreePage.PropertiesFilter.Filter(property.Name);
        }

        private class PropertyComparer : IComparer<PropertyViewModel>
        {
            public static PropertyComparer Instance { get; } = new PropertyComparer();

            public int Compare(PropertyViewModel? x, PropertyViewModel? y)
            {
                var groupX = GroupIndex(x?.Group);
                var groupY = GroupIndex(y?.Group);

                if (groupX != groupY)
                {
                    return groupX - groupY;
                }
                else
                {
                    return string.CompareOrdinal(x?.Name, y?.Name);
                }
            }

            private int GroupIndex(string? group)
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

        public void ApplySelectedProperty()
        {
            var selectedProperty = SelectedProperty;
            var selectedEntity = SelectedEntity;
            var selectedEntityName = SelectedEntityName;
            if (selectedEntity == null 
                || selectedProperty == null 
                || selectedProperty.Type == typeof(string)
                || selectedProperty.Type.IsValueType
                )
                return;

            object? property;
            if (selectedProperty.Key is AvaloniaProperty avaloniaProperty)
            {
                property = (_selectedEntity as IControl)?.GetValue(avaloniaProperty);
            }
            else
            {
                property = selectedEntity.GetType().GetProperties()
                     .FirstOrDefault(pi => pi.Name == selectedProperty.Name
                           && pi.DeclaringType == selectedProperty.DeclaringType
                           && pi.PropertyType.Name == selectedProperty.Type.Name)
                     ?.GetValue(selectedEntity);
            }
            if (property == null) return;
            _selectedEntitiesStack.Push((Name:selectedEntityName!,Entry:selectedEntity));
            NavigateToProperty(property, selectedProperty.Name);
        }

        public void ApplyParentProperty()
        {
            if (_selectedEntitiesStack.Any())
            {
                var property = _selectedEntitiesStack.Pop();
                NavigateToProperty(property.Entry, property.Name);
            }
        }
        
        protected  void NavigateToProperty(object o, string entityName)
        {
            var oldSelectedEntity = SelectedEntity;
            if (oldSelectedEntity is IAvaloniaObject ao1)
            {
                ao1.PropertyChanged -= ControlPropertyChanged;
            }
            else if (oldSelectedEntity is INotifyPropertyChanged inpc1)
            {
                inpc1.PropertyChanged -= ControlPropertyChanged;
            }
      
            SelectedEntity = o;
            SelectedEntityName = entityName;
            SelectedEntityType = o.ToString();
            var properties = GetAvaloniaProperties(o)
                .Concat(GetClrProperties(o))
                .OrderBy(x => x, PropertyComparer.Instance)
                .ThenBy(x => x.Name)
                .ToList();

            _propertyIndex = properties.GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.ToList());

            var view = new DataGridCollectionView(properties);
            view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(AvaloniaPropertyViewModel.Group)));
            view.Filter = FilterProperty;
            PropertiesView = view;

            if (o is IAvaloniaObject ao2)
            {
                ao2.PropertyChanged += ControlPropertyChanged;
            }
            else if (o is INotifyPropertyChanged inpc2)
            {
                inpc2.PropertyChanged += ControlPropertyChanged;
            }
        }
    }
}

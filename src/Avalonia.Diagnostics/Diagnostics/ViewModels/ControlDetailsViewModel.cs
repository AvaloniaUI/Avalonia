using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class ControlDetailsViewModel : ViewModelBase, IDisposable, IClassesChangedListener
    {
        private readonly AvaloniaObject _avaloniaObject;
        private readonly ISet<string> _pinnedProperties;
        private IDictionary<object, PropertyViewModel[]>? _propertyIndex;
        private PropertyViewModel? _selectedProperty;
        private DataGridCollectionView? _propertiesView;
        private bool _snapshotFrames;
        private bool _showInactiveFrames;
        private string? _framesStatus;
        private object? _selectedEntity;
        private readonly Stack<(string Name, object Entry)> _selectedEntitiesStack = new();
        private string? _selectedEntityName;
        private string? _selectedEntityType;
        private bool _showImplementedInterfaces;
        // new DataGridPathGroupDescription(nameof(AvaloniaPropertyViewModel.Group))
        private readonly static IReadOnlyList<DataGridPathGroupDescription> GroupDescriptors = new DataGridPathGroupDescription[]
        {
            new DataGridPathGroupDescription(nameof(AvaloniaPropertyViewModel.Group))
        };

        private readonly static IReadOnlyList<DataGridSortDescription> SortDescriptions = new DataGridSortDescription[]
        {
            new DataGridComparerSortDescription(PropertyComparer.Instance!, ListSortDirection.Ascending),
        };

        public ControlDetailsViewModel(TreePageViewModel treePage, AvaloniaObject avaloniaObject, ISet<string> pinnedProperties)
        {
            _avaloniaObject = avaloniaObject;
            _pinnedProperties = pinnedProperties;
            TreePage = treePage;
            Layout = avaloniaObject is Visual visual
                ? new ControlLayoutViewModel(visual)
                : default;

            NavigateToProperty(_avaloniaObject, (_avaloniaObject as Control)?.Name ?? _avaloniaObject.ToString());

            AppliedFrames = new ObservableCollection<ValueFrameViewModel>();
            PseudoClasses = new ObservableCollection<PseudoClassViewModel>();

            if (avaloniaObject is StyledElement styledElement)
            {
                styledElement.Classes.AddListener(this);

                var pseudoClassAttributes = styledElement.GetType().GetCustomAttributes<PseudoClassesAttribute>(true);

                foreach (var classAttribute in pseudoClassAttributes)
                {
                    foreach (var className in classAttribute.PseudoClasses)
                    {
                        PseudoClasses.Add(new PseudoClassViewModel(className, styledElement));
                    }
                }

                var styleDiagnostics = styledElement.GetValueStoreDiagnostic();

                var clipboard = TopLevel.GetTopLevel(_avaloniaObject as Visual)?.Clipboard;

                foreach (var appliedStyle in styleDiagnostics.AppliedFrames.OrderBy(s => s.Priority))
                {
                    AppliedFrames.Add(new ValueFrameViewModel(styledElement, appliedStyle, clipboard));
                }

                UpdateStyles();
            }
        }

        public bool CanNavigateToParentProperty => _selectedEntitiesStack.Count >= 1;

        public TreePageViewModel TreePage { get; }

        public DataGridCollectionView? PropertiesView
        {
            get => _propertiesView;
            private set => RaiseAndSetIfChanged(ref _propertiesView, value);
        }

        public ObservableCollection<ValueFrameViewModel> AppliedFrames { get; }

        public ObservableCollection<PseudoClassViewModel> PseudoClasses { get; }

        public object? SelectedEntity
        {
            get => _selectedEntity;
            set => RaiseAndSetIfChanged(ref _selectedEntity, value);
        }

        public string? SelectedEntityName
        {
            get => _selectedEntityName;
            set => RaiseAndSetIfChanged(ref _selectedEntityName, value);
        }

        public string? SelectedEntityType
        {
            get => _selectedEntityType;
            set => RaiseAndSetIfChanged(ref _selectedEntityType, value);
        }

        public PropertyViewModel? SelectedProperty
        {
            get => _selectedProperty;
            set => RaiseAndSetIfChanged(ref _selectedProperty, value);
        }

        public bool SnapshotFrames
        {
            get => _snapshotFrames;
            set => RaiseAndSetIfChanged(ref _snapshotFrames, value);
        }

        public bool ShowInactiveFrames
        {
            get => _showInactiveFrames;
            set => RaiseAndSetIfChanged(ref _showInactiveFrames, value);
        }

        public string? FramesStatus
        {
            get => _framesStatus;
            set => RaiseAndSetIfChanged(ref _framesStatus, value);
        }

        public ControlLayoutViewModel? Layout { get; }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(SnapshotFrames))
            {
                if (!SnapshotFrames)
                {
                    UpdateStyles();
                }
            }
        }

        public void UpdateStyleFilters()
        {
            foreach (var style in AppliedFrames)
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
            if (_avaloniaObject is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged -= ControlPropertyChanged;
            }

            if (_avaloniaObject is AvaloniaObject ao)
            {
                ao.PropertyChanged -= ControlPropertyChanged;
            }

            if (_avaloniaObject is StyledElement se)
            {
                se.Classes.RemoveListener(this);
            }
        }

        private static IEnumerable<PropertyViewModel> GetAvaloniaProperties(object o)
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

        private static IEnumerable<PropertyViewModel> GetClrProperties(object o, bool showImplementedInterfaces)
        {
            foreach (var p in GetClrProperties(o, o.GetType()))
            {
                yield return p;
            }

            if (showImplementedInterfaces)
            {
                foreach (var i in o.GetType().GetInterfaces())
                {
                    foreach (var p in GetClrProperties(o, i))
                    {
                        yield return p;
                    }
                }
            }
        }

        private static IEnumerable<PropertyViewModel> GetClrProperties(object o, Type t)
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

            Layout?.ControlPropertyChanged(sender, e);
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

            if (!SnapshotFrames)
            {
                Dispatcher.UIThread.Post(UpdateStyles);
            }
        }

        void IClassesChangedListener.Changed()
        {
            if (!SnapshotFrames)
            {
                Dispatcher.UIThread.Post(UpdateStyles);
            }
        }

        private void UpdateStyles()
        {
            int activeCount = 0;

            foreach (var style in AppliedFrames)
            {
                style.Update();

                if (style.IsActive)
                {
                    activeCount++;
                }
            }

            var propertyBuckets = new Dictionary<AvaloniaProperty, List<SetterViewModel>>();

            foreach (var style in AppliedFrames.Reverse())
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

            FramesStatus = $"Value Frames ({activeCount}/{AppliedFrames.Count} active)";
        }

        private bool FilterProperty(object arg)
        {
            return !(arg is PropertyViewModel property) || TreePage.PropertiesFilter.Filter(property.Name);
        }

        private class PropertyComparer : IComparer<PropertyViewModel>, IComparer
        {
            public static PropertyComparer Instance { get; } = new PropertyComparer();

            public int Compare(PropertyViewModel? x, PropertyViewModel? y)
            {
                if (x is null && y is null)
                    return 0;

                if (x is null && y is not null)
                    return -1;

                if (x is not null && y is null)
                    return 1;

                var groupX = GroupIndex(x!.Group);
                var groupY = GroupIndex(y!.Group);

                if (groupX != groupY)
                {
                    return groupX - groupY;
                }
                else
                {
                    return string.CompareOrdinal(x.Name, y.Name);
                }
            }

            private static int GroupIndex(string? group)
            {
                switch (group)
                {
                    case "Pinned":
                        return -1;
                    case "Properties":
                        return 0;
                    case "Attached Properties":
                        return 1;
                    case "CLR Properties":
                        return 2;
                    default:
                        return 3;
                }
            }

            public int Compare(object? x, object? y) =>
                Compare(x as PropertyViewModel, y as PropertyViewModel);
        }

        private static IEnumerable<PropertyInfo> GetAllPublicProperties(Type type)
        {
            return type
                .GetProperties()
                .Concat(type.GetInterfaces().SelectMany(i => i.GetProperties()));
        }

        public void NavigateToSelectedProperty()
        {
            var selectedProperty = SelectedProperty;
            var selectedEntity = SelectedEntity;
            var selectedEntityName = SelectedEntityName;
            if (selectedEntity == null
                || selectedProperty == null
                || selectedProperty.PropertyType == typeof(string)
                || selectedProperty.PropertyType.IsValueType)
                return;

            object? property = null;

            switch (selectedProperty)
            {
                case AvaloniaPropertyViewModel avaloniaProperty:

                    property = (_selectedEntity as Control)?.GetValue(avaloniaProperty.Property);

                    break;

                case ClrPropertyViewModel clrProperty:
                    {
                        property = GetAllPublicProperties(selectedEntity.GetType())
                            .FirstOrDefault(pi => clrProperty.Property == pi)?
                            .GetValue(selectedEntity);

                        break;
                    }
            }

            if (property == null)
                return;

            _selectedEntitiesStack.Push((Name: selectedEntityName!, Entry: selectedEntity));

            var propertyName = selectedProperty.Name;

            //Strip out interface names
            if (propertyName.LastIndexOf('.') is var p && p != -1)
            {
                propertyName = propertyName.Substring(p + 1);
            }

            NavigateToProperty(property, selectedEntityName + "." + propertyName);

            RaisePropertyChanged(nameof(CanNavigateToParentProperty));
        }

        public void NavigateToParentProperty()
        {
            if (_selectedEntitiesStack.Count > 0)
            {
                var property = _selectedEntitiesStack.Pop();
                NavigateToProperty(property.Entry, property.Name);

                RaisePropertyChanged(nameof(CanNavigateToParentProperty));
            }
        }

        protected void NavigateToProperty(object o, string? entityName)
        {
            var oldSelectedEntity = SelectedEntity;

            switch (oldSelectedEntity)
            {
                case AvaloniaObject ao1:
                    ao1.PropertyChanged -= ControlPropertyChanged;
                    break;

                case INotifyPropertyChanged inpc1:
                    inpc1.PropertyChanged -= ControlPropertyChanged;
                    break;
            }

            SelectedEntity = o;
            SelectedEntityName = entityName;
            SelectedEntityType = o.ToString();

            var properties = GetAvaloniaProperties(o)
                .Concat(GetClrProperties(o, _showImplementedInterfaces))
                .Do(p =>
                    {
                        p.IsPinned = _pinnedProperties.Contains(p.FullName);
                    })
                .ToArray();

            _propertyIndex = properties
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.ToArray());

            var view = new DataGridCollectionView(properties);
            view.GroupDescriptions.AddRange(GroupDescriptors);
            view.SortDescriptions.AddRange(SortDescriptions);
            view.Filter = FilterProperty;
            PropertiesView = view;

            switch (o)
            {
                case AvaloniaObject ao2:
                    ao2.PropertyChanged += ControlPropertyChanged;
                    break;

                case INotifyPropertyChanged inpc2:
                    inpc2.PropertyChanged += ControlPropertyChanged;
                    break;
            }
        }

        internal void SelectProperty(AvaloniaProperty property)
        {
            SelectedProperty = null;

            if (SelectedEntity != _avaloniaObject)
            {
                NavigateToProperty(
                    _avaloniaObject,
                    (_avaloniaObject as Control)?.Name ?? _avaloniaObject.ToString());
            }

            if (PropertiesView is null)
            {
                return;
            }

            foreach (object o in PropertiesView)
            {
                if (o is AvaloniaPropertyViewModel propertyVm && propertyVm.Property == property)
                {
                    SelectedProperty = propertyVm;

                    break;
                }
            }
        }

        internal void UpdatePropertiesView(bool showImplementedInterfaces)
        {
            _showImplementedInterfaces = showImplementedInterfaces;
            SelectedProperty = null;
            NavigateToProperty(_avaloniaObject, (_avaloniaObject as Control)?.Name ?? _avaloniaObject.ToString());
        }

        public void TogglePinnedProperty(object parameter)
        {
            if (parameter is PropertyViewModel model)
            {
                var fullname = model.FullName;
                if (_pinnedProperties.Contains(fullname))
                {
                    _pinnedProperties.Remove(fullname);
                    model.IsPinned = false;
                }
                else
                {
                    _pinnedProperties.Add(fullname);
                    model.IsPinned = true;
                }
                PropertiesView?.Refresh();
            }
        }
    }
}

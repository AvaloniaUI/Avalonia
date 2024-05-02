using System;
using System.ComponentModel;
using Avalonia.Data;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class AvaloniaPropertyViewModel : PropertyViewModel
    {
        private readonly AvaloniaObject _target;
        private Type _assignedType;
        private object? _value;
        private string _priority;
        private string _group;
        private readonly Type _propertyType;

#nullable disable
        // Remove "nullable disable" after MemberNotNull will work on our CI.
        public AvaloniaPropertyViewModel(AvaloniaObject o, AvaloniaProperty property)
#nullable restore
        {
            _target = o;
            Property = property;

            Name = property.IsAttached ?
                $"[{property.OwnerType.Name}.{property.Name}]" :
                property.Name;
            DeclaringType = property.OwnerType;
            _propertyType = property.PropertyType;
            Update();
        }

        public AvaloniaProperty Property { get; }
        public override object Key => Property;
        public override string Name { get; }
        public override bool? IsAttached => Property.IsAttached;
        public override string Priority => _priority;
        public override Type AssignedType => _assignedType;

        public override object? Value
        {
            get => _value;
            set
            {
                try
                {
                    _target.SetValue(Property, value);
                    Update();
                }
                catch { }
            }
        }

        public override string Group => IsPinned ? "Pinned" : _group;

        public override Type? DeclaringType { get; }
        public override Type PropertyType => _propertyType;
        public override bool IsReadonly => Property.IsReadOnly;

        // [MemberNotNull(nameof(_type), nameof(_group), nameof(_priority))]
        public override void Update()
        {
            if (Property.IsDirect)
            {
                object? value;
                Type? valueType = null;

                try
                {
                    value = _target.GetValue(Property);
                    valueType = value?.GetType();
                }
                catch (Exception e)
                {
                    value = e.GetBaseException();
                }

                RaiseAndSetIfChanged(ref _value, value, nameof(Value));
                RaiseAndSetIfChanged(ref _assignedType, valueType ?? Property.PropertyType, nameof(AssignedType));
                RaiseAndSetIfChanged(ref _priority, "Direct", nameof(Priority));

                _group = "Properties";
            }
            else
            {
                object? value;
                Type? valueType = null;
                BindingPriority? priority = null;

                try
                {
                    var diag = _target.GetDiagnostic(Property);

                    value = diag.Value;
                    valueType = value?.GetType();
                    priority = diag.Priority;
                }
                catch (Exception e)
                {
                    value = e.GetBaseException();
                }

                RaiseAndSetIfChanged(ref _value, value, nameof(Value));
                RaiseAndSetIfChanged(ref _assignedType, valueType ?? Property.PropertyType, nameof(AssignedType));

                if (priority != null)
                {
                    RaiseAndSetIfChanged(ref _priority, priority.ToString()!, nameof(Priority));
                    RaiseAndSetIfChanged(ref _group, IsAttached == true ? "Attached Properties" : "Properties", nameof(Group));
                }
                else
                {
                    RaiseAndSetIfChanged(ref _priority, "Unset", nameof(Priority));
                    RaiseAndSetIfChanged(ref _group, "Unset", nameof(Group));
                }
            }
            RaisePropertyChanged(nameof(Type));
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName == nameof(IsPinned))
            {
                RaisePropertyChanged(nameof(Group));
            }
        }
    }
}

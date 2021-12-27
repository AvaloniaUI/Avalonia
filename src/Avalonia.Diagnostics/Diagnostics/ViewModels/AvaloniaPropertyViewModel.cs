namespace Avalonia.Diagnostics.ViewModels
{
    internal class AvaloniaPropertyViewModel : PropertyViewModel
    {
        private readonly AvaloniaObject _target;
        private System.Type _assignedType;
        private object? _value;
        private string _priority;
        private string _group;
        private readonly System.Type _propertyType;

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
        public override bool? IsAttached => 
            Property.IsAttached;

        public override string Priority =>
            _priority;

        public override System.Type AssignedType => _assignedType;

        public override string? Value
        {
            get => ConvertToString(_value);
            set
            {
                try
                {
                    var convertedValue = ConvertFromString(value, Property.PropertyType);
                    _target.SetValue(Property, convertedValue);
                    Update();
                }
                catch { }
            }
        }

        public override string Group => _group;

        public override System.Type? DeclaringType { get; }
        public override System.Type PropertyType => _propertyType;

        // [MemberNotNull(nameof(_type), nameof(_group), nameof(_priority))]
        public override void Update()
        {
            if (Property.IsDirect)
            {
                RaiseAndSetIfChanged(ref _value, _target.GetValue(Property), nameof(Value));
                RaiseAndSetIfChanged(ref _assignedType,_value?.GetType() ?? Property.PropertyType, nameof(AssignedType));
                RaiseAndSetIfChanged(ref _priority, "Direct", nameof(Priority));

                _group = "Properties";
            }
            else
            {
                var val = _target.GetDiagnostic(Property);

                RaiseAndSetIfChanged(ref _value, val?.Value, nameof(Value));
                RaiseAndSetIfChanged(ref _assignedType, _value?.GetType() ?? Property.PropertyType, nameof(AssignedType));

                if (val != null)
                {
                    RaiseAndSetIfChanged(ref _priority, val.Priority.ToString(), nameof(Priority));
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
    }
}

using System.ComponentModel;
using Avalonia.Collections;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class AvaloniaPropertyViewModel : PropertyViewModel
    {
        private readonly AvaloniaObject _target;
        private string _type;
        private object _value;
        private string _priority;
        private string _group;

        public AvaloniaPropertyViewModel(AvaloniaObject o, AvaloniaProperty property)
        {
            _target = o;
            Property = property;

            Name = property.IsAttached ?
                $"[{property.OwnerType.Name}.{property.Name}]" :
                property.Name;

            if (property.IsDirect)
            {
                _group = "Properties";
                Priority = "Direct";
            }

            Update();
        }

        public AvaloniaProperty Property { get; }
        public override object Key => Property;
        public override string Name { get; }
        public bool IsAttached => Property.IsAttached;

        public string Priority
        {
            get => _priority;
            private set => RaiseAndSetIfChanged(ref _priority, value);
        }

        public override string Type => _type;

        public override string Value
        {
            get => ConvertToString(_value);
            set
            {
                try
                {
                    var convertedValue = ConvertFromString(value, Property.PropertyType);
                    _target.SetValue(Property, convertedValue);
                }
                catch { }
            }
        }

        public override string Group
        {
            get => _group;
        }

        public override void Update()
        {
            if (Property.IsDirect)
            {
                RaiseAndSetIfChanged(ref _value, _target.GetValue(Property), nameof(Value));
                RaiseAndSetIfChanged(ref _type, _value?.GetType().Name, nameof(Type));
            }
            else
            {
                var val = _target.GetDiagnostic(Property);

                RaiseAndSetIfChanged(ref _value, val?.Value, nameof(Value));
                RaiseAndSetIfChanged(ref _type, _value?.GetType().Name, nameof(Type));

                if (val != null)
                {
                    SetGroup(IsAttached ? "Attached Properties" : "Properties");
                    Priority = val.Priority.ToString();
                }
                else
                {
                    SetGroup(Priority = "Unset");
                }
            }
        }

        private void SetGroup(string group)
        {
            RaiseAndSetIfChanged(ref _group, group, nameof(Group));
        }
    }
}

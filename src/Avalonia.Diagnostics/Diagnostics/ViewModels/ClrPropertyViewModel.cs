using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia.Data.Converters;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class ClrPropertyViewModel : PropertyViewModel
    {
        private readonly object _target;
        private object _value;
        private TypeConverter _converter;

        public ClrPropertyViewModel(object o, PropertyInfo property)
        {
            _target = o;
            Property = property;

            if (!property.DeclaringType.IsInterface)
            {
                Name = property.Name;
            }
            else
            {
                Name = property.DeclaringType.Name + '.' + property.Name;
            }

            Update();
        }

        public PropertyInfo Property { get; }
        public override object Key => Name;
        public override string Name { get; }
        public override string Group => "CLR Properties";

        public override string Value 
        {
            get
            {
                if (_value == null)
                {
                    return "(null)";
                }

                return Converter?.CanConvertTo(typeof(string)) == true ?
                    Converter.ConvertToString(_value) :
                    _value.ToString();
            }
            set
            {
                try
                {
                    var convertedValue = Converter?.CanConvertFrom(typeof(string)) == true ?
                        Converter.ConvertFromString(value) :
                        DefaultValueConverter.Instance.ConvertBack(value, Property.PropertyType, null, CultureInfo.CurrentCulture);
                    Property.SetValue(_target, convertedValue);
                }
                catch { }
            }
        }

        private TypeConverter Converter
        {
            get
            {
                if (_converter == null)
                {
                    _converter = TypeDescriptor.GetConverter(_value.GetType());
                }

                return _converter;
            }
        }

        public override void Update()
        {
            var val = Property.GetValue(_target);
            RaiseAndSetIfChanged(ref _value, val, nameof(Value));
        }
    }
}

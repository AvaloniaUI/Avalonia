using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Markup.Xaml.Converters
{
    public class NullableTypeConverter<T> : TypeConverter where T : TypeConverter, new()
    {
        private TypeConverter _inner;

        public NullableTypeConverter()
        {
            _inner = new T();
        }

        public NullableTypeConverter(TypeConverter inner)
        {
            _inner = inner;
        }

        
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value == null)
                return null;
            return _inner.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value == null)
                return null;
            if (value as string == "")
                return null;
            return _inner.ConvertFrom(context, culture, value);
        }
        
        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            return _inner.CreateInstance(context, propertyValues);
        }
        
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return _inner.GetStandardValuesSupported(context);
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return _inner.GetStandardValuesExclusive(context);
        }

        public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
        {
            return _inner.GetCreateInstanceSupported(context);
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return _inner.GetPropertiesSupported(context);
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return _inner.GetStandardValues(context);
        }

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            return _inner.GetProperties(context, value, attributes);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return _inner.CanConvertTo(context, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return _inner.CanConvertFrom(context, sourceType);
        }

        public override bool IsValid(ITypeDescriptorContext context, object value)
        {
            return _inner.IsValid(context, value);
        }
    }
}

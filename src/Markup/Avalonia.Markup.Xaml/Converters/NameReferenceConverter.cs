using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Markup.Xaml.Converters
{
    public class NameReferenceConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            //var nameResolver = (IXamlNameResolver)context.GetService(typeof(IXamlNameResolver));
            //if (nameResolver == null)
            //{
            //    throw new InvalidOperationException(SR.Get(SRID.MissingNameResolver));
            //}

            //string name = value as string;
            //if (String.IsNullOrEmpty(name))
            //{
            //    throw new InvalidOperationException(SR.Get(SRID.MustHaveName));
            //}
            //object obj = nameResolver.Resolve(name);
            //if (obj == null)
            //{
            //    string[] names = new string[] { name };
            //    obj = nameResolver.GetFixupToken(names, true);
            //}
            return null; //obj;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            //if (context == null || (context.GetService(typeof(IXamlNameProvider)) as IXamlNameProvider) == null)
            //{
            //    return false;
            //}

            //if (destinationType == typeof(string))
            //{
            //    return true;
            //}

            return base.CanConvertTo(context, destinationType);

        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            //if (context == null)
            //{
            //    throw new ArgumentNullException(nameof(context));
            //}

            //var nameProvider = (IXamlNameProvider)context.GetService(typeof(IXamlNameProvider));
            //if (nameProvider == null)
            //{
            //    throw new InvalidOperationException(SR.Get(SRID.MissingNameProvider));
            //}

            //return nameProvider.GetName(value);
            return null;
        }
    }
}

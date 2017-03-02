using System;
using System.Linq;
using System.Reflection;
using Avalonia.Markup.Xaml.Templates;
using Portable.Xaml.ComponentModel;
using avm = Avalonia.Metadata;
using pm = Portable.Xaml.Markup;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    public class AvaloniaMemberAttributeProvider : ICustomAttributeProvider
    {
        public AvaloniaMemberAttributeProvider(MemberInfo info)
        {
            _info = info;
        }

        public object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            object[] result = null;

            if (attributeType == typeof(pm.XamlDeferLoadAttribute))
            {
                var attr = GetXamlDeferLoadAttribute(inherit);

                if (attr != null)
                {
                    result = new object[] { attr };
                }
            }

            if (result == null || result.Length == 0)
            {
                var attr = _info.GetCustomAttributes(attributeType, inherit);
                return (attr as object[]) ?? attr.ToArray();
            }
            else
            {
                return result;
            }
        }

        public bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        private readonly MemberInfo _info;

        private Attribute GetXamlDeferLoadAttribute(bool inherit)
        {
            var result = _info.GetCustomAttributes(typeof(avm.TemplateContentAttribute), inherit)
                                            .Cast<avm.TemplateContentAttribute>()
                                            .FirstOrDefault();

            if (result == null)
            {
                return null;
            }

            return new pm.XamlDeferLoadAttribute(typeof(TemplateLoader), typeof(TemplateContent));
        }
    }
}
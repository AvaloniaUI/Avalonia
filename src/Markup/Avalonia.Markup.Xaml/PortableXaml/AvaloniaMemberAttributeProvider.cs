using Portable.Xaml.ComponentModel;
using System;
using System.Linq;
using System.Reflection;
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
            Attribute result = null;

            
            if (attributeType == typeof(pm.XamlDeferLoadAttribute))
            {
                result = _info.GetCustomAttribute<avm.TemplateContentAttribute>(inherit)
                                .ToPortableXaml();
            }
            else if (attributeType == typeof(pm.AmbientAttribute))
            {
                result = _info.GetCustomAttribute<avm.AmbientAttribute>(inherit)
                                .ToPortableXaml();
            }
            else if(attributeType == typeof(pm.DependsOnAttribute))
            {
                result = _info.GetCustomAttribute<avm.DependsOnAttribute>(inherit)
                                .ToPortableXaml();
            }

            if (result == null)
            {
                var attr = _info.GetCustomAttributes(attributeType, inherit);
                return (attr as object[]) ?? attr.ToArray();
            }
            else
            {
                return new object[] { result };
            }
        }

        public bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        private readonly MemberInfo _info;
    }
}
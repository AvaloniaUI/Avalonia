using System;
using System.Linq;
using System.Reflection;
using Avalonia.Markup.Xaml.Context;
using Portable.Xaml;
using Portable.Xaml.ComponentModel;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    internal class AvaloniaXamlSchemaContext : XamlSchemaContext
    {
        public AvaloniaXamlSchemaContext(IRuntimeTypeProvider typeProvider)
            : base(typeProvider.ReferencedAssemblies)
        {
            _avaloniaTypeProvider = typeProvider;
        }

        private IRuntimeTypeProvider _avaloniaTypeProvider;

        protected override XamlType GetXamlType(string xamlNamespace, string name, params XamlType[] typeArguments)
        {
            XamlType type = null;
            try
            {
                type = ResolveXamlTypeName(xamlNamespace, name, typeArguments, false);

                if (type == null)
                {
                    type = base.GetXamlType(xamlNamespace, name, typeArguments);
                }
            }
            catch (Exception e)
            {
                //TODO: log or wrap exception
                throw e;
            }
            return type;
        }

        private XamlType ResolveXamlTypeName(string xmlNamespace, string xmlLocalName, XamlType[] typeArguments, bool required)
        {
            Type[] genArgs = null;
            if (typeArguments != null && typeArguments.Any())
            {
                genArgs = typeArguments.Select(t => t?.UnderlyingType).ToArray();

                if (genArgs.Any(t => t == null))
                {
                    return null;
                }
            }

            Type type = _avaloniaTypeProvider.FindType(xmlNamespace, xmlLocalName, genArgs);

            if (type == null)
            {
                return null;
            }

            return GetXamlType(type);
        }

        protected override ICustomAttributeProvider GetCustomAttributeProvider(Type type)
                                    => new AvaloniaTypeAttributeProvider(type);

        protected override ICustomAttributeProvider GetCustomAttributeProvider(MemberInfo member)
                                    => new AvaloniaMemberAttributeProvider(member);

        public override XamlType GetXamlType(Type type)
        {
            if (type.FullName.StartsWith("Avalonia."))
            {
                return new AvaloniaXamlType(type, this);
            }

            return base.GetXamlType(type);
        }
    }
}
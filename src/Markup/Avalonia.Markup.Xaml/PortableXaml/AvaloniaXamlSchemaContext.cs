using System;
using System.Collections.Generic;
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
                //let's try the simple types
                //in Portable xaml xmlns:sys='clr-namespace:System;assembly=mscorlib'
                //and sys:Double is not resolved
                return ResolveSimpleTypeName(xmlNamespace, xmlLocalName);
            }

            return GetXamlType(type);
        }

        #region Workaround for bug in Portablexaml system types like double,int etc ...

        private static Type[] _simpleTypes = new Type[]
        {
            typeof(bool),
            typeof(byte),
            typeof(char),
            typeof(decimal),
            typeof(double),
            typeof(Int16),
            typeof(Int32),
            typeof(Int64),
            typeof(float),
            typeof(string),
            typeof(TimeSpan),
            typeof(Uri),
        };

        private static Dictionary<Tuple<string, string>, XamlType> _simpleXamlTypes;

        private static XamlType ResolveSimpleTypeName(string xmlNamespace, string xmlLocalName)
        {
            if (_simpleXamlTypes == null)
            {
                _simpleXamlTypes = new Dictionary<Tuple<string, string>, XamlType>();

                foreach (var type in _simpleTypes)
                {
                    string asmName = type.GetTypeInfo().Assembly.GetName().Name;
                    string ns = $"clr-namespace:{type.Namespace};assembly={asmName}";
                    var xamlType = XamlLanguage.AllTypes.First(t => t.UnderlyingType == type);
                    _simpleXamlTypes.Add(new Tuple<string, string>(ns, type.Name), xamlType);
                }
            }

            XamlType result;

            var key = new Tuple<string, string>(xmlNamespace, xmlLocalName);

            _simpleXamlTypes.TryGetValue(key, out result);

            return result;
        }

        #endregion Workaround for bug in Portablexaml system types like double,int etc ...

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
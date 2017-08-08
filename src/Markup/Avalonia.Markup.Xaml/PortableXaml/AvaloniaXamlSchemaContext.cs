using Avalonia.Data;
using Avalonia.Markup.Xaml.Context;
using Avalonia.Markup.Xaml.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.Styling;
using Portable.Xaml;
using Portable.Xaml.ComponentModel;
using System.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    internal class AvaloniaXamlSchemaContext : XamlSchemaContext
    {
        public static AvaloniaXamlSchemaContext Create(IRuntimeTypeProvider typeProvider = null)
        {
            return new AvaloniaXamlSchemaContext(typeProvider ?? new AvaloniaRuntimeTypeProvider());
        }

        private AvaloniaXamlSchemaContext(IRuntimeTypeProvider typeProvider)
        //better not set the references assemblies
        //TODO: check this on iOS
        //: base(typeProvider.ReferencedAssemblies)
        {
            Contract.Requires<ArgumentNullException>(typeProvider != null);

            _avaloniaTypeProvider = typeProvider;
        }

        private IRuntimeTypeProvider _avaloniaTypeProvider;

        protected internal override XamlType GetXamlType(string xamlNamespace, string name, params XamlType[] typeArguments)
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

            // MarkupExtension type could omit "Extension" part in XML name.
            Type type = _avaloniaTypeProvider.FindType(xmlNamespace,
                                                        xmlLocalName,
                                                        genArgs) ??
                        _avaloniaTypeProvider.FindType(xmlNamespace,
                                                        xmlLocalName + "Extension",
                                                        genArgs);

            if (type != null)
            {
                Type extType;
                if (_wellKnownExtensionTypes.TryGetValue(type, out extType))
                {
                    type = extType;
                }
            }

            if (type == null)
            {
                //let's try the simple types
                //in Portable xaml like xmlns:sys='clr-namespace:System;assembly=mscorlib'
                //and sys:Double is not resolved properly
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

        //in Portable xaml like xmlns:sys='clr-namespace:System;assembly=mscorlib'
        //and sys:Double is not resolved properly
        [Obsolete("TODO: remove once it's fixed in Portable.xaml")]
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

        protected internal override ICustomAttributeProvider GetCustomAttributeProvider(Type type)
                                    => new AvaloniaTypeAttributeProvider(type);

        protected internal override ICustomAttributeProvider GetCustomAttributeProvider(MemberInfo member)
                                    => new AvaloniaMemberAttributeProvider(member);

        public override XamlType GetXamlType(Type type)
        {
            XamlType result = null;

            if (_cachedTypes.TryGetValue(type, out result))
            {
                return result;
            }

            _cachedTypes[type] = result = GetAvaloniaXamlType(type) ?? base.GetXamlType(type);

            return result;
        }

        private static readonly Dictionary<Type, Type> _wellKnownExtensionTypes = new Dictionary<Type, Type>()
        {
            { typeof(Binding), typeof(BindingExtension) },
            { typeof(StyleInclude), typeof(StyleIncludeExtension) },
        };

        private XamlType GetAvaloniaXamlType(Type type)
        {
            //if type is extension get the original type to check
            var origType = _wellKnownExtensionTypes.FirstOrDefault(v => v.Value == type).Key;

            if (typeof(IBinding).GetTypeInfo().IsAssignableFrom((origType ?? type).GetTypeInfo()))
            {
                return new BindingXamlType(type, this);
            }

            if (origType != null ||
                typeof(AvaloniaObject).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                return new AvaloniaXamlType(type, this);
            }

            return null;
        }

        protected internal override XamlMember GetAttachableProperty(string attachablePropertyName, MethodInfo getter, MethodInfo setter)
        {
            var key = MemberKey.Create(getter ?? setter, attachablePropertyName, "a");

            XamlMember result;

            if (_cachedMembers.TryGetValue(key, out result))
            {
                return result;
            }

            var type = (getter ?? setter).DeclaringType;

            var prop = AvaloniaPropertyRegistry.Instance.GetAttached(type)
                    .FirstOrDefault(v => v.Name == attachablePropertyName);

            if (prop != null)
            {
                result = new AvaloniaAttachedPropertyXamlMember(
                                        prop, attachablePropertyName,
                                        getter, setter, this);
            }

            if (result == null)
            {
                result = base.GetAttachableProperty(attachablePropertyName, getter, setter);
            }

            return _cachedMembers[key] = result;
        }

        protected internal override XamlMember GetProperty(PropertyInfo pi)
        {
            Type objType = pi.DeclaringType;
            string name = pi.Name;

            XamlMember result;

            var key = MemberKey.Create(pi, "p");

            if (_cachedMembers.TryGetValue(key, out result))
            {
                return result;
            }

            var avProp = AvaloniaPropertyRegistry.Instance.FindRegistered(objType, name);

            if (avProp != null)
            {
                result = new AvaloniaPropertyXamlMember(avProp, pi, this);
            }

            if (result == null)
            {
                result = new PropertyXamlMember(pi, this);
            }

            return _cachedMembers[key] = result;
        }

        private Dictionary<Type, XamlType> _cachedTypes = new Dictionary<Type, XamlType>();

        private Dictionary<MemberKey, XamlMember> _cachedMembers = new Dictionary<MemberKey, XamlMember>();

        private struct MemberKey
        {
            public static MemberKey Create(MemberInfo m, string name, string memberType)
            {
                return new MemberKey(m.DeclaringType, name, memberType);
            }

            public static MemberKey Create(MemberInfo m, string memberType)
            {
                return Create(m, m.Name, memberType);
            }

            public MemberKey(Type type, object member, string memberType)
            {
                Type = type;
                Member = member;
                MemberType = memberType;
            }

            public Type Type { get; }

            public object Member { get; }

            public string MemberType { get; }

            public override string ToString()
            {
                return $"{MemberType}:{Type.Namespace}:{Type.Name}.{Member}";
            }
        }
    }
}
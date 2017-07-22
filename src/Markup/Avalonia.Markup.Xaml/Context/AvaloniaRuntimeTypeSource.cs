namespace Avalonia.Markup.Xaml.Context
{
#if OMNIXAML
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Collections;
    using Controls;
    using Controls.Primitives;
    using Controls.Templates;
    using Converters;
    using Data;
    using Glass;
    using Input;
    using Media;
    using Media.Imaging;
    using Metadata;
    using OmniXaml;
    using OmniXaml.Builder;
    using OmniXaml.TypeConversion;
    using OmniXaml.Typing;
    using Avalonia.Styling;
    using Platform;
    using Templates;

    public class AvaloniaRuntimeTypeSource : IRuntimeTypeSource
    {
        private readonly RuntimeTypeSource inner;

        public AvaloniaRuntimeTypeSource(ITypeFactory typeFactory)
        {
            var namespaceRegistry = new AvaloniaNamespaceRegistry();
            var featureProvider = new AvaloniaTypeFeatureProvider();
            var typeRepository = new AvaloniaTypeRepository(namespaceRegistry, typeFactory, featureProvider);

            inner = new RuntimeTypeSource(typeRepository, namespaceRegistry);
        }

        public Namespace GetNamespace(string name)
        {
            return inner.GetNamespace(name);
        }

        public Namespace GetNamespaceByPrefix(string prefix)
        {
            return inner.GetNamespaceByPrefix(prefix);
        }

        public void RegisterPrefix(PrefixRegistration prefixRegistration)
        {
            inner.RegisterPrefix(prefixRegistration);
        }

        public void AddNamespace(XamlNamespace xamlNamespace)
        {
            inner.AddNamespace(xamlNamespace);
        }

        public IEnumerable<PrefixRegistration> RegisteredPrefixes => inner.RegisteredPrefixes;

        public XamlType GetByType(Type type)
        {
            return inner.GetByType(type);
        }

        public XamlType GetByQualifiedName(string qualifiedName)
        {
            return inner.GetByQualifiedName(qualifiedName);
        }

        public XamlType GetByPrefix(string prefix, string typeName)
        {
            return inner.GetByPrefix(prefix, typeName);
        }

        public XamlType GetByFullAddress(XamlTypeName xamlTypeName)
        {
            return inner.GetByFullAddress(xamlTypeName);
        }

        public Member GetMember(PropertyInfo propertyInfo)
        {
            return inner.GetMember(propertyInfo);
        }

        public AttachableMember GetAttachableMember(string name, MethodInfo getter, MethodInfo setter)
        {
            return inner.GetAttachableMember(name, getter, setter);
        }
    }
#endif
}
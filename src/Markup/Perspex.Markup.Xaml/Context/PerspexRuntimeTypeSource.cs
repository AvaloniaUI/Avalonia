namespace Perspex.Markup.Xaml.Context
{
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
    using Perspex.Styling;
    using Platform;
    using Templates;

    public class PerspexRuntimeTypeSource : IRuntimeTypeSource
    {
        private const string PerspexNs = "https://github.com/perspex";
        private readonly RuntimeTypeSource inner;

        public PerspexRuntimeTypeSource(ITypeFactory typeFactory)
        {
            var namespaceRegistry = CreateNamespaceRegistry();

            var featureProvider = new TypeFeatureProvider(GetConverterProvider());
            LoadFeatureProvider(featureProvider);
            var typeRepository = new PerspexTypeRepository(namespaceRegistry, typeFactory, featureProvider);

            inner = new RuntimeTypeSource(typeRepository, namespaceRegistry);
        }

        private void LoadFeatureProvider(ITypeFeatureProvider featureProvider)
        {
            featureProvider.RegisterMetadata(new GenericMetadata<Visual>().WithRuntimeNameProperty(d => d.Name));
            featureProvider.RegisterMetadata(new GenericMetadata<Setter>().WithMemberDependency(x => x.Value, x => x.Property));
            featureProvider.RegisterMetadata(
                new GenericMetadata<SelectingItemsControl>()
                .WithMemberDependency(x => x.SelectedIndex, x => x.Items)
                .WithMemberDependency(x => x.SelectedItem, x => x.Items));

            RegisteContentProperties(featureProvider);
        }

        private static void RegisteContentProperties(ITypeFeatureProvider featureProvider)
        {
            var typeAndAttribute = from type in ScannedAssemblies.AllExportedTypes()
                                   let properties = type.GetTypeInfo().DeclaredProperties
                                   from property in properties
                                   let attr = property.GetCustomAttribute<ContentAttribute>()
                                   where attr != null
                                   select new { Type = type, Property = property, ContentAttribute = attr };

            foreach (var t in typeAndAttribute)
            {
                featureProvider.RegisterMetadata(t.Type, new Metadata {ContentProperty = t.Property.Name});
            }
        }

        private static INamespaceRegistry CreateNamespaceRegistry()
        {
            var xamlNamespaceRegistry = new NamespaceRegistry();

            var forcedAssemblies = new[]
            {
                typeof(Binding),
                typeof(Control),
                typeof(IValueConverter),
                typeof(Style),
            }.Select(t => t.GetTypeInfo().Assembly);

            foreach (var nsa in
                forcedAssemblies
                    .Concat(PerspexLocator.Current.GetService<IPclPlatformWrapper>().GetLoadedAssemblies())
                    .Distinct()
                    .SelectMany(asm
                        => asm.GetCustomAttributes<XmlnsDefinitionAttribute>().Select(attr => new { asm, attr }))
                    .GroupBy(entry => entry.attr.XmlNamespace))
            {
                var def = XamlNamespace.Map(nsa.Key)
                    .With(nsa.GroupBy(x => x.asm).Select(
                        a => Route.Assembly(a.Key)
                            .WithNamespaces(a.Select(entry => entry.attr.ClrNamespace).ToList())
                        ));
                xamlNamespaceRegistry.AddNamespace(def);
            }

            xamlNamespaceRegistry.RegisterPrefix(new PrefixRegistration(string.Empty, PerspexNs));

            return xamlNamespaceRegistry;
        }

        private static ITypeConverterProvider GetConverterProvider()
        {
            var typeConverterProvider = new TypeConverterProvider();
            var converters = new[]
            {
                new TypeConverterRegistration(typeof(IBitmap), new BitmapTypeConverter()),
                new TypeConverterRegistration(typeof(Brush), new BrushTypeConverter()),
                new TypeConverterRegistration(typeof(Color), new ColorTypeConverter()),
                new TypeConverterRegistration(typeof(Classes), new ClassesTypeConverter()),
                new TypeConverterRegistration(typeof(ColumnDefinitions), new ColumnDefinitionsTypeConverter()),
                new TypeConverterRegistration(typeof(Geometry), new GeometryTypeConverter()),
                new TypeConverterRegistration(typeof(GridLength), new GridLengthTypeConverter()),
                new TypeConverterRegistration(typeof(KeyGesture), new KeyGestureConverter()),
                new TypeConverterRegistration(typeof(PerspexList<double>), new PerspexListTypeConverter<double>()),
                new TypeConverterRegistration(typeof(IMemberSelector), new MemberSelectorTypeConverter()),
                new TypeConverterRegistration(typeof(Point), new PointTypeConverter()),
                new TypeConverterRegistration(typeof(IList<Point>), new PointsListTypeConverter()),
                new TypeConverterRegistration(typeof(PerspexProperty), new PerspexPropertyTypeConverter()),
                new TypeConverterRegistration(typeof(RelativePoint), new RelativePointTypeConverter()),
                new TypeConverterRegistration(typeof(RelativeRect), new RelativeRectTypeConverter()),
                new TypeConverterRegistration(typeof(RowDefinitions), new RowDefinitionsTypeConverter()),
                new TypeConverterRegistration(typeof(Selector), new SelectorTypeConverter()),
                new TypeConverterRegistration(typeof(Thickness), new ThicknessTypeConverter()),
                new TypeConverterRegistration(typeof(TimeSpan), new TimeSpanTypeConverter()),
                new TypeConverterRegistration(typeof(Uri), new UriTypeConverter()),
                new TypeConverterRegistration(typeof(Cursor), new CursorTypeConverter())
            };

            typeConverterProvider.AddAll(converters);
            return typeConverterProvider;
        }

        private static IEnumerable<Assembly> ScannedAssemblies => new List<Assembly>
        {
            typeof(PerspexObject).GetTypeInfo().Assembly,
            typeof(Control).GetTypeInfo().Assembly,
            typeof(Style).GetTypeInfo().Assembly,
            typeof(DataTemplate).GetTypeInfo().Assembly,
            typeof(SolidColorBrush).GetTypeInfo().Assembly,
        }.Distinct();

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
}
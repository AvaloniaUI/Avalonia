





namespace Perspex.Markup.Xaml.Context
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Reflection;
    using Controls;
    using Converters;
    using DataBinding;
    using Glass;
    using MarkupExtensions;
    using Media;
    using Media.Imaging;
    using OmniXaml;
    using OmniXaml.Builder;
    using OmniXaml.TypeConversion;
    using OmniXaml.Typing;
    using Templates;

    public class PerspexWiringContext : IWiringContext
    {
        private readonly WiringContext context;
        private const string PerspexNs = "https://github.com/grokys/Perspex";

        public PerspexWiringContext(ITypeFactory typeFactory)
        {
            var featureProvider = new TypeFeatureProvider(GetContentPropertyProvider(), GetConverterProvider());

            var xamlNamespaceRegistry = CreateXamlNamespaceRegistry();
            var perspexPropertyBinder = new PerspexPropertyBinder(featureProvider.ConverterProvider);
            var xamlTypeRepository = new PerspexTypeRepository(xamlNamespaceRegistry, typeFactory, featureProvider, perspexPropertyBinder);
            var typeContext = new TypeContext(xamlTypeRepository, xamlNamespaceRegistry, typeFactory);
            this.context = new WiringContext(typeContext, featureProvider);
        }

        private static XamlNamespaceRegistry CreateXamlNamespaceRegistry()
        {
            var xamlNamespaceRegistry = new XamlNamespaceRegistry();

            var rootType = typeof(Control);
            var bindingType = typeof(BindingExtension);
            var templateType = typeof(XamlDataTemplate);

            var definitionForRoot = XamlNamespace
                .Map(PerspexNs)
                .With(
                    new[]
                    {
                        Route.Assembly(rootType.GetTypeInfo().Assembly).WithNamespaces(
                            new[]
                            {
                                rootType.Namespace
                            }),
                        Route.Assembly(bindingType.GetTypeInfo().Assembly).WithNamespaces(
                            new[]
                            {
                                bindingType.Namespace,
                            }),
                        Route.Assembly(templateType.GetTypeInfo().Assembly).WithNamespaces(
                            new[]
                            {
                                templateType.Namespace,
                            })
                    });

            foreach (var ns in new List<XamlNamespace> { definitionForRoot })
            {
                xamlNamespaceRegistry.AddNamespace(ns);
            }

            xamlNamespaceRegistry.RegisterPrefix(new PrefixRegistration(string.Empty, PerspexNs));

            return xamlNamespaceRegistry;
        }

        private static ITypeConverterProvider GetConverterProvider()
        {
            var typeConverterProvider = new TypeConverterProvider();
            var converters = new[]
            {
                new TypeConverterRegistration(typeof(Bitmap), new BitmapConverter()),
                new TypeConverterRegistration(typeof(Brush), new BrushConverter()),
                new TypeConverterRegistration(typeof(ColumnDefinitions), new ColumnDefinitionsTypeConverter()),
                new TypeConverterRegistration(typeof(GridLength), new GridLengthTypeConverter()),
                new TypeConverterRegistration(typeof(RowDefinitions), new RowDefinitionsTypeConverter()),
                new TypeConverterRegistration(typeof(Thickness), new ThicknessConverter()),
            };

            typeConverterProvider.AddAll(converters);
            return typeConverterProvider;
        }

        private static ContentPropertyProvider GetContentPropertyProvider()
        {
            var contentPropertyProvider = new ContentPropertyProvider();
            var contentProperties = new Collection<ContentPropertyDefinition>
            {
                new ContentPropertyDefinition(typeof(ContentControl), "Content"),
                new ContentPropertyDefinition(typeof(Decorator), "Child"),
                new ContentPropertyDefinition(typeof(ItemsControl), "Items"),
                new ContentPropertyDefinition(typeof(Panel), "Children"),
                new ContentPropertyDefinition(typeof(TextBlock), "Text"),
                new ContentPropertyDefinition(typeof(TextBox), "Text"),
                new ContentPropertyDefinition(typeof(XamlDataTemplate), "Content"),
            };

            contentPropertyProvider.AddAll(contentProperties);

            return contentPropertyProvider;
        }

        public ITypeContext TypeContext => this.context.TypeContext;

        public ITypeFeatureProvider FeatureProvider => this.context.FeatureProvider;
    }
}
// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Glass;
using OmniXaml;
using OmniXaml.Builder;
using OmniXaml.TypeConversion;
using OmniXaml.Typing;
using Perspex.Controls;
using Perspex.Markup.Xaml.Templates;
using Perspex.Markup.Xaml.Converters;
using Perspex.Markup.Xaml.DataBinding;
using Perspex.Markup.Xaml.MarkupExtensions;
using Perspex.Media;
using Perspex.Media.Imaging;
using Perspex.Metadata;
using Perspex.Platform;
using Perspex.Styling;
using Splat;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexWiringContext : IWiringContext
    {
        private readonly WiringContext _context;
        private const string PerspexNs = "https://github.com/perspex";

        public PerspexWiringContext(ITypeFactory typeFactory)
        {
            var featureProvider = new TypeFeatureProvider(GetContentPropertyProvider(), GetConverterProvider());

            var xamlNamespaceRegistry = CreateXamlNamespaceRegistry();
            var perspexPropertyBinder = new PerspexPropertyBinder(featureProvider.ConverterProvider);
            var xamlTypeRepository = new PerspexTypeRepository(xamlNamespaceRegistry, typeFactory, featureProvider, perspexPropertyBinder);
            var typeContext = new TypeContext(xamlTypeRepository, xamlNamespaceRegistry, typeFactory);
            _context = new WiringContext(typeContext, featureProvider);
        }

        private static XamlNamespaceRegistry CreateXamlNamespaceRegistry()
        {
            var xamlNamespaceRegistry = new XamlNamespaceRegistry();

            var forcedAssemblies = new[]
            {
                typeof (Control),
                typeof(Style)
            }.Select(t => t.GetTypeInfo().Assembly);

            foreach (var nsa in 
                forcedAssemblies
                    .Concat(Locator.Current.GetService<IPclPlatformWrapper>().GetLoadedAssemblies())
                    .Distinct()
                    .SelectMany(asm
                        => asm.GetCustomAttributes<XmlnsDefinitionAttribute>().Select(attr => new {asm, attr}))
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
                new TypeConverterRegistration(typeof(Bitmap), new BitmapTypeConverter()),
                new TypeConverterRegistration(typeof(Brush), new BrushTypeConverter()),
                new TypeConverterRegistration(typeof(Color), new ColorTypeConverter()),
                new TypeConverterRegistration(typeof(Classes), new ClassesTypeConverter()),
                new TypeConverterRegistration(typeof(ColumnDefinitions), new ColumnDefinitionsTypeConverter()),
                new TypeConverterRegistration(typeof(GridLength), new GridLengthTypeConverter()),
                new TypeConverterRegistration(typeof(Point), new PointTypeConverter()),
                new TypeConverterRegistration(typeof(PerspexProperty), new PerspexPropertyTypeConverter()),
                new TypeConverterRegistration(typeof(RelativePoint), new RelativePointTypeConverter()),
                new TypeConverterRegistration(typeof(RowDefinitions), new RowDefinitionsTypeConverter()),
                new TypeConverterRegistration(typeof(Thickness), new ThicknessTypeConverter()),
                new TypeConverterRegistration(typeof(Selector), new SelectorTypeConverter()),
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
                new ContentPropertyDefinition(typeof(GradientBrush), "GradientStops"),
                new ContentPropertyDefinition(typeof(Panel), "Children"),
                new ContentPropertyDefinition(typeof(Style), "Setters"),
                new ContentPropertyDefinition(typeof(TextBlock), "Text"),
                new ContentPropertyDefinition(typeof(TextBox), "Text"),
                new ContentPropertyDefinition(typeof(XamlDataTemplate), "Content"),
            };

            contentPropertyProvider.AddAll(contentProperties);

            return contentPropertyProvider;
        }

        public ITypeContext TypeContext => _context.TypeContext;

        public ITypeFeatureProvider FeatureProvider => _context.FeatureProvider;
    }
}
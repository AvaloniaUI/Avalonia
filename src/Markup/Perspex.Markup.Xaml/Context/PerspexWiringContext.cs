// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using Glass;
using OmniXaml;
using OmniXaml.Builder;
using OmniXaml.TypeConversion;
using OmniXaml.Typing;
using Perspex.Collections;
using Perspex.Controls;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Input;
using Perspex.Markup.Xaml.Converters;
using Perspex.Markup.Xaml.Data;
using Perspex.Media;
using Perspex.Media.Imaging;
using Perspex.Metadata;
using Perspex.Platform;
using Perspex.Styling;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexWiringContext : WiringContext
    {
        private const string PerspexNs = "https://github.com/perspex";

        public PerspexWiringContext(ITypeFactory typeFactory)
            : this(typeFactory, new TypeFeatureProvider(GetContentPropertyProvider(), GetConverterProvider()))
        {
        }

        public PerspexWiringContext(ITypeFactory typeFactory, TypeFeatureProvider featureProvider)
            : base(CreateTypeContext(typeFactory, featureProvider), featureProvider)
        {
        }

        private static ITypeContext CreateTypeContext(ITypeFactory typeFactory, TypeFeatureProvider featureProvider)
        {
            var xamlNamespaceRegistry = CreateXamlNamespaceRegistry();
            var typeRepository = new PerspexTypeRepository(xamlNamespaceRegistry, typeFactory, featureProvider);

            typeRepository.RegisterMetadata(new GenericMetadata<Visual>().WithRuntimeNameProperty(d => d.Name));
            typeRepository.RegisterMetadata(new GenericMetadata<Setter>().WithMemberDependency(x => x.Value, x => x.Property));
            typeRepository.RegisterMetadata(
                new GenericMetadata<SelectingItemsControl>()
                .WithMemberDependency(x => x.SelectedIndex, x => x.Items)
                .WithMemberDependency(x => x.SelectedItem, x => x.Items));

            return new TypeContext(typeRepository, xamlNamespaceRegistry, typeFactory);
        }

        private static XamlNamespaceRegistry CreateXamlNamespaceRegistry()
        {
            var xamlNamespaceRegistry = new XamlNamespaceRegistry();

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

        private static IContentPropertyProvider GetContentPropertyProvider()
        {
            return new PerspexContentPropertyProvider();
        }
    }
}
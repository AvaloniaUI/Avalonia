// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using Glass;
using OmniXaml;
using OmniXaml.Builder;
using OmniXaml.TypeConversion;
using OmniXaml.Typing;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Converters;
using Avalonia.Markup.Xaml.Data;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Styling;
using System.Collections.Generic;

namespace Avalonia.Markup.Xaml.Context
{
    public class AvaloniaWiringContext : WiringContext
    {
        private const string AvaloniaNs = "https://github.com/avaloniaui";

        public AvaloniaWiringContext(ITypeFactory typeFactory)
            : this(typeFactory, new TypeFeatureProvider(GetContentPropertyProvider(), GetConverterProvider()))
        {
        }

        public AvaloniaWiringContext(ITypeFactory typeFactory, TypeFeatureProvider featureProvider)
            : base(CreateTypeContext(typeFactory, featureProvider), featureProvider)
        {
        }

        private static ITypeContext CreateTypeContext(ITypeFactory typeFactory, TypeFeatureProvider featureProvider)
        {
            var xamlNamespaceRegistry = CreateXamlNamespaceRegistry();
            var typeRepository = new AvaloniaTypeRepository(xamlNamespaceRegistry, typeFactory, featureProvider);

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
                    .Concat(AvaloniaLocator.Current.GetService<IPclPlatformWrapper>().GetLoadedAssemblies())
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
            xamlNamespaceRegistry.RegisterPrefix(new PrefixRegistration(string.Empty, AvaloniaNs));

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
                new TypeConverterRegistration(typeof(AvaloniaList<double>), new AvaloniaListTypeConverter<double>()),
                new TypeConverterRegistration(typeof(IMemberSelector), new MemberSelectorTypeConverter()),
                new TypeConverterRegistration(typeof(Point), new PointTypeConverter()),
                new TypeConverterRegistration(typeof(IList<Point>), new PointsListTypeConverter()),
                new TypeConverterRegistration(typeof(AvaloniaProperty), new AvaloniaPropertyTypeConverter()),
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
            return new AvaloniaContentPropertyProvider();
        }
    }
}
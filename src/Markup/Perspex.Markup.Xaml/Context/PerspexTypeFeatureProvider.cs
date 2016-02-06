// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OmniXaml;
using OmniXaml.Builder;
using OmniXaml.TypeConversion;
using OmniXaml.TypeConversion.BuiltInConverters;
using OmniXaml.Typing;
using Perspex.Collections;
using Perspex.Controls;
using Perspex.Controls.Templates;
using Perspex.Input;
using Perspex.Markup.Xaml.Converters;
using Perspex.Media;
using Perspex.Media.Imaging;
using Perspex.Metadata;
using Perspex.Styling;
using OmniMetadata = OmniXaml.Typing.Metadata;

namespace Perspex.Markup.Xaml.Context
{
    public class PerspexTypeFeatureProvider : ITypeFeatureProvider
    {
        private Dictionary<Type, OmniMetadata> _metadata = 
            new Dictionary<Type, OmniMetadata>();

        private Dictionary<Type, TypeConverterRegistration> _typeConverters =
            new Dictionary<Type, TypeConverterRegistration>();

        public IEnumerable<TypeConverterRegistration> TypeConverters => _typeConverters.Values;

        public PerspexTypeFeatureProvider()
        {
            RegisterTypeConverters();
        }

        public string GetContentPropertyName(Type type)
        {
            return GetMetadata(type)?.ContentProperty;
        }

        public OmniMetadata GetMetadata(Type type)
        {
            OmniMetadata result;

            if (!_metadata.TryGetValue(type, out result))
            {
                result = LoadMetadata(type);
                _metadata.Add(type, result);
            }

            return result;
        }

        public ITypeConverter GetTypeConverter(Type type)
        {
            TypeConverterRegistration result;
            _typeConverters.TryGetValue(type, out result);
            return result?.TypeConverter;
        }

        public void RegisterMetadata(Type type, OmniMetadata metadata)
        {
            _metadata.Add(type, metadata);
        }

        public void RegisterTypeConverter(Type type, ITypeConverter converter)
        {
            _typeConverters.Add(type, new TypeConverterRegistration(type, converter));
        }

        private static OmniMetadata LoadMetadata(Type type)
        {
            return new OmniMetadata
            {
                ContentProperty = GetContentProperty(type),
                PropertyDependencies = GetPropertyDependencies(type),
                RuntimePropertyName = type.GetRuntimeProperty("Name")?.Name,
            };
        }

        private static string GetContentProperty(Type type)
        {
            while (type != null)
            {
                var properties = type.GetTypeInfo().DeclaredProperties
                    .Where(x => x.GetCustomAttribute<ContentAttribute>() != null);
                string result = null;

                foreach (var property in properties)
                {
                    if (result != null)
                    {
                        throw new Exception($"Content property defined more than once on {type}.");
                    }

                    result = property.Name;
                }
                
                if (result != null)
                {
                    return result;
                }

                type = type.GetTypeInfo().BaseType;
            }

            return null;
        }

        private static DependencyRegistrations GetPropertyDependencies(Type type)
        {
            var result = new List<DependencyRegistration>();

            while (type != null)
            {
                var registrations = type.GetTypeInfo().DeclaredProperties
                    .Select(x => new
                    {
                        Property = x.Name,
                        Attributes = x.GetCustomAttributes<DependsOnAttribute>(),
                    })
                    .Where(x => x.Attributes.Any())
                    .SelectMany(x => x.Attributes.Select(y => new DependencyRegistration(x.Property, y.Name)));

                result.AddRange(registrations);
                type = type.GetTypeInfo().BaseType;
            }

            return result.Count > 0 ? new DependencyRegistrations(result) : null;
        }

        private void RegisterTypeConverters()
        {
            // HACK: For now these are hard-coded. Hopefully when the .NET Standard Platform
            // is available we can use the System.ComponentModel.TypeConverters so don't want to
            // spend time for now inventing a mechanism to register type converters if it's all
            // going to change.
            RegisterTypeConverter(typeof(string), new StringTypeConverter());
            RegisterTypeConverter(typeof(int), new IntTypeConverter());
            RegisterTypeConverter(typeof(long), new IntTypeConverter());
            RegisterTypeConverter(typeof(short), new IntTypeConverter());
            RegisterTypeConverter(typeof(double), new DoubleTypeConverter());
            RegisterTypeConverter(typeof(float), new IntTypeConverter());
            RegisterTypeConverter(typeof(bool), new BooleanConverter());
            RegisterTypeConverter(typeof(Type), new TypeTypeConverter());

            RegisterTypeConverter(typeof(IBitmap), new BitmapTypeConverter());
            RegisterTypeConverter(typeof(Brush), new BrushTypeConverter());
            RegisterTypeConverter(typeof(Color), new ColorTypeConverter());
            RegisterTypeConverter(typeof(Classes), new ClassesTypeConverter());
            RegisterTypeConverter(typeof(ColumnDefinitions), new ColumnDefinitionsTypeConverter());
            RegisterTypeConverter(typeof(Geometry), new GeometryTypeConverter());
            RegisterTypeConverter(typeof(GridLength), new GridLengthTypeConverter());
            RegisterTypeConverter(typeof(KeyGesture), new KeyGestureConverter());
            RegisterTypeConverter(typeof(PerspexList<double>), new PerspexListTypeConverter<double>());
            RegisterTypeConverter(typeof(IMemberSelector), new MemberSelectorTypeConverter());
            RegisterTypeConverter(typeof(Point), new PointTypeConverter());
            RegisterTypeConverter(typeof(IList<Point>), new PointsListTypeConverter());
            RegisterTypeConverter(typeof(PerspexProperty), new PerspexPropertyTypeConverter());
            RegisterTypeConverter(typeof(RelativePoint), new RelativePointTypeConverter());
            RegisterTypeConverter(typeof(RelativeRect), new RelativeRectTypeConverter());
            RegisterTypeConverter(typeof(RowDefinitions), new RowDefinitionsTypeConverter());
            RegisterTypeConverter(typeof(Selector), new SelectorTypeConverter());
            RegisterTypeConverter(typeof(Thickness), new ThicknessTypeConverter());
            RegisterTypeConverter(typeof(TimeSpan), new TimeSpanTypeConverter());
            RegisterTypeConverter(typeof(Uri), new UriTypeConverter());
            RegisterTypeConverter(typeof(Cursor), new CursorTypeConverter());
        }
    }
}
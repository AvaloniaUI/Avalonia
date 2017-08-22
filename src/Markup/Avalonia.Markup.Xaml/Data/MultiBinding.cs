// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Metadata;
using System.Windows.Markup;

namespace Avalonia.Markup.Xaml.Data
{
    /// <summary>
    /// A XAML binding that calculates an aggregate value from multiple child <see cref="Bindings"/>.
    /// </summary>
    [ContentProperty(nameof(Bindings))]
    public class MultiBinding : IBinding
    {
        /// <summary>
        /// Gets the collection of child bindings.
        /// </summary>
        [Content]
        public IList<IBinding> Bindings { get; set; } = new List<IBinding>();

        /// <summary>
        /// Gets or sets the <see cref="IValueConverter"/> to use.
        /// </summary>
        public IMultiValueConverter Converter { get; set; }

        /// <summary>
        /// Gets or sets the value to use when the binding is unable to produce a value.
        /// </summary>
        public object FallbackValue { get; set; }

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode { get; set; } = BindingMode.OneWay;

        /// <summary>
        /// Gets or sets the binding priority.
        /// </summary>
        public BindingPriority Priority { get; set; }
        
        /// <summary>
        /// Gets or sets the relative source for the binding.
        /// </summary>
        public RelativeSource RelativeSource { get; set; }

        /// <inheritdoc/>
        public InstancedBinding Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object anchor = null,
            bool enableDataValidation = false)
        {
            if (Converter == null)
            {
                throw new NotSupportedException("MultiBinding without Converter not currently supported.");
            }

            var targetType = targetProperty?.PropertyType ?? typeof(object);
            var result = new BehaviorSubject<object>(AvaloniaProperty.UnsetValue);
            var children = Bindings.Select(x => x.Initiate(target, null));
            var input = children.Select(x => x.Subject).CombineLatest().Select(x => ConvertValue(x, targetType));
            input.Subscribe(result);
            return new InstancedBinding(result, Mode, Priority);
        }

        /// <summary>
        /// Applies a binding subject to a property on an instance.
        /// </summary>
        /// <param name="target">The target instance.</param>
        /// <param name="property">The target property.</param>
        /// <param name="subject">The binding subject.</param>
        internal void Bind(IAvaloniaObject target, AvaloniaProperty property, ISubject<object> subject)
        {
            var mode = Mode == BindingMode.Default ?
                property.GetMetadata(target.GetType()).DefaultBindingMode : Mode;

            switch (mode)
            {
                case BindingMode.Default:
                case BindingMode.OneWay:
                    target.Bind(property, subject, Priority);
                    break;
                case BindingMode.TwoWay:
                    throw new NotSupportedException("TwoWay MultiBinding not currently supported.");
                case BindingMode.OneTime:
                    target.GetObservable(Control.DataContextProperty).Subscribe(dataContext =>
                    {
                        subject.Take(1).Subscribe(x => target.SetValue(property, x, Priority));
                    });                    
                    break;
                case BindingMode.OneWayToSource:
                    target.GetObservable(property).Subscribe(subject);
                    break;
            }
        }

        private object ConvertValue(IList<object> values, Type targetType)
        {
            var converted = Converter.Convert(values, targetType, null, CultureInfo.CurrentCulture);

            if (converted == AvaloniaProperty.UnsetValue && FallbackValue != null)
            {
                converted = FallbackValue;
            }

            return converted;
        }
    }
}
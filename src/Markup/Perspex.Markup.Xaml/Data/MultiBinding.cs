// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Perspex.Controls;
using Perspex.Metadata;

namespace Perspex.Markup.Xaml.Data
{
    /// <summary>
    /// A XAML binding that calculates an aggregate value from multiple child <see cref="Bindings"/>.
    /// </summary>
    public class MultiBinding : IXamlBinding
    {
        /// <summary>
        /// Gets the collection of child bindings.
        /// </summary>
        [Content]
        public IList<IXamlBinding> Bindings { get; set; } = new List<IXamlBinding>();

        /// <summary>
        /// Gets or sets the <see cref="IValueConverter"/> to use.
        /// </summary>
        public IMultiValueConverter Converter { get; set; }

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

        /// <summary>
        /// Applies the binding to a property on an instance.
        /// </summary>
        /// <param name="instance">The target instance.</param>
        /// <param name="property">The target property.</param>
        public void Bind(IObservablePropertyBag instance, PerspexProperty property)
        {
            var subject = CreateSubject(instance, property.PropertyType);

            if (subject != null)
            {
                Bind(instance, property, subject);
            }
        }

        /// <summary>
        /// Creates a subject that can be used to get and set the value of the binding.
        /// </summary>
        /// <param name="target">The target instance.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="targetIsDataContext">
        /// Whether the target property is the DataContext property.
        /// </param>
        /// <returns>An <see cref="ISubject{object}"/>.</returns>
        public ISubject<object> CreateSubject(
            IObservablePropertyBag target, 
            Type targetType,
            bool targetIsDataContext = false)
        {
            if (Converter == null)
            {
                throw new NotSupportedException("MultiBinding without Converter not currently supported.");
            }

            var result = new BehaviorSubject<object>(PerspexProperty.UnsetValue);
            var children = Bindings.Select(x => x.CreateSubject(target, typeof(object)));
            var input = children.CombineLatest().Select(x =>
                Converter.Convert(x, targetType, null, CultureInfo.CurrentUICulture));
            input.Subscribe(result);
            return result;
        }

        /// <summary>
        /// Applies a binding subject to a property on an instance.
        /// </summary>
        /// <param name="target">The target instance.</param>
        /// <param name="property">The target property.</param>
        /// <param name="subject">The binding subject.</param>
        internal void Bind(IObservablePropertyBag target, PerspexProperty property, ISubject<object> subject)
        {
            var mode = Mode == BindingMode.Default ?
                property.DefaultBindingMode : Mode;

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
    }
}
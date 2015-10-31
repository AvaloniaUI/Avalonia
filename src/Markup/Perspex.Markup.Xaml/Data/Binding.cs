// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using OmniXaml.TypeConversion;
using Perspex.Controls;
using Perspex.Markup.Data;

namespace Perspex.Markup.Xaml.Data
{
    /// <summary>
    /// A XAML binding.
    /// </summary>
    public class Binding : IXamlBinding
    {
        /// <summary>
        /// Gets or sets the <see cref="IValueConverter"/> to use.
        /// </summary>
        public IValueConverter Converter { get; set; }

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the binding priority.
        /// </summary>
        public BindingPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the relative source for the binding.
        /// </summary>
        public RelativeSource RelativeSource { get; set; }

        /// <summary>
        /// Gets or sets the binding path.
        /// </summary>
        public string SourcePropertyPath { get; set; }

        /// <summary>
        /// Applies the binding to a property on an instance.
        /// </summary>
        /// <param name="instance">The target instance.</param>
        /// <param name="property">The target property.</param>
        public void Bind(IObservablePropertyBag instance, PerspexProperty property)
        {
            var subject = CreateSubject(
                instance, 
                property.PropertyType,
                property == Control.DataContextProperty);

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
            ExpressionObserver observer;

            if (RelativeSource == null || RelativeSource.Mode == RelativeSourceMode.DataContext)
            {
                observer = CreateDataContextExpressionSubject(target, targetIsDataContext);
            }
            else if (RelativeSource.Mode == RelativeSourceMode.TemplatedParent)
            {
                observer = CreateTemplatedParentExpressionSubject(target);
            }
            else
            {
                throw new NotSupportedException();
            }

            return new ExpressionSubject(
                observer, 
                targetType, 
                Converter ?? DefaultValueConverter.Instance);
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
                    target.BindTwoWay(property, subject, Priority);
                    break;
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

        private ExpressionObserver CreateDataContextExpressionSubject(
            IObservablePropertyBag target,
            bool targetIsDataContext)
        {
            var dataContextHost = targetIsDataContext ?
                target.InheritanceParent as IObservablePropertyBag : target;

            if (dataContextHost != null)
            {
                var result = new ExpressionObserver(
                    () => dataContextHost.GetValue(Control.DataContextProperty),
                    GetExpression());
                dataContextHost.GetObservable(Control.DataContextProperty).Subscribe(x =>
                    result.UpdateRoot());
                return result;
            }
            else
            {
                throw new InvalidOperationException(
                    "Cannot bind to DataContext of object with no parent.");
            }
        }

        private ExpressionObserver CreateTemplatedParentExpressionSubject(IObservablePropertyBag target)
        {
            var result = new ExpressionObserver(
                () => target.GetValue(Control.TemplatedParentProperty),
                GetExpression());

            if (target.GetValue(Control.TemplatedParentProperty) == null)
            {
                // TemplatedParent should only be set once, so only listen for the first non-null
                // value.
                target.GetObservable(Control.TemplatedParentProperty)
                    .Where(x => x != null)
                    .Take(1)
                    .Subscribe(x => result.UpdateRoot());
            }

            return result;
        }

        private string GetExpression()
        {
            return SourcePropertyPath == null || SourcePropertyPath == "." ?
                string.Empty : SourcePropertyPath;
        }
    }
}
// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        /// Gets or sets a parameter to pass to <see cref="Converter"/>.
        /// </summary>
        public object ConverterParameter { get; set; }

        /// <summary>
        /// Gets or sets the name of the element to use as the binding source.
        /// </summary>
        public string ElementName { get; set; }

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
        public string Path { get; set; }

        /// <summary>
        /// Applies the binding to a property on an instance.
        /// </summary>
        /// <param name="instance">The target instance.</param>
        /// <param name="property">The target property.</param>
        public void Bind(IObservablePropertyBag instance, PerspexProperty property)
        {
            Contract.Requires<ArgumentNullException>(instance != null);
            Contract.Requires<ArgumentNullException>(property != null);

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
            Contract.Requires<ArgumentNullException>(target != null);
            Contract.Requires<ArgumentNullException>(targetType != null);

            var pathInfo = ParsePath(Path);
            ValidateState(pathInfo);

            ExpressionObserver observer;

            if (pathInfo.ElementName != null || ElementName != null)
            {
                observer = CreateElementSubject(
                    (IControl)target, 
                    pathInfo.ElementName ?? ElementName, 
                    pathInfo.Path);
            }
            else if (RelativeSource == null || RelativeSource.Mode == RelativeSourceMode.DataContext)
            {
                observer = CreateDataContextSubject(
                    target, 
                    pathInfo.Path,
                    targetIsDataContext);
            }
            else if (RelativeSource.Mode == RelativeSourceMode.TemplatedParent)
            {
                observer = CreateTemplatedParentSubject(
                    target,
                    pathInfo.Path);
            }
            else
            {
                throw new NotSupportedException();
            }

            return new ExpressionSubject(
                observer,
                targetType,
                Converter ?? DefaultValueConverter.Instance,
                ConverterParameter);
        }

        /// <summary>
        /// Applies a binding subject to a property on an instance.
        /// </summary>
        /// <param name="target">The target instance.</param>
        /// <param name="property">The target property.</param>
        /// <param name="subject">The binding subject.</param>
        internal void Bind(IObservablePropertyBag target, PerspexProperty property, ISubject<object> subject)
        {
            Contract.Requires<ArgumentNullException>(target != null);
            Contract.Requires<ArgumentNullException>(property != null);
            Contract.Requires<ArgumentNullException>(subject != null);

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

        private static PathInfo ParsePath(string path)
        {
            var result = new PathInfo();

            if (string.IsNullOrWhiteSpace(path) || path == ".")
            {
                result.Path = string.Empty;
            }
            else if (path.StartsWith("#"))
            {
                var dot = path.IndexOf('.');

                if (dot != -1)
                {
                    result.Path = path.Substring(dot + 1);
                    result.ElementName = path.Substring(1, dot - 1);
                }
                else
                {
                    result.Path = string.Empty;
                    result.ElementName = path.Substring(1);
                }
            }
            else
            {
                result.Path = path;
            }

            return result;
        }

        private void ValidateState(PathInfo pathInfo)
        {
            if (pathInfo.ElementName != null && ElementName != null)
            {
                throw new InvalidOperationException(
                    "ElementName property cannot be set when an #elementName path is provided.");
            }

            if ((pathInfo.ElementName != null || ElementName != null) &&
                RelativeSource != null)
            {
                throw new InvalidOperationException(
                    "ElementName property cannot be set with a RelativeSource.");
            }
        }

        private ExpressionObserver CreateDataContextSubject(
            IObservablePropertyBag target,
            string path,
            bool targetIsDataContext)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            var dataContextHost = targetIsDataContext ?
                target.InheritanceParent as IObservablePropertyBag : target;

            if (dataContextHost != null)
            {
                var result = new ExpressionObserver(
                    () => dataContextHost.GetValue(Control.DataContextProperty),
                    path);
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

        private ExpressionObserver CreateTemplatedParentSubject(
            IObservablePropertyBag target,
            string path)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            var result = new ExpressionObserver(
                () => target.GetValue(Control.TemplatedParentProperty),
                path);

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

        private ExpressionObserver CreateElementSubject(
            IControl target,
            string elementName,
            string path)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            var result = new ExpressionObserver(
                ControlLocator.Track(target, elementName), 
                path);
            return result;
        }

        private IControl LookupNamedControl(IControl target)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            var nameScope = target.FindNameScope();

            if (nameScope == null)
            {
                throw new InvalidOperationException(
                    "Could not find name scope for ElementName binding.");
            }

            return nameScope.Find<IControl>(ElementName);
        }

        private class PathInfo
        {
            public string Path { get; set; }
            public string ElementName { get; set; }
        }
    }
}
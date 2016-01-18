// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Perspex.Controls;
using Perspex.Data;
using Perspex.Markup.Data;

namespace Perspex.Markup.Xaml.Data
{
    /// <summary>
    /// A XAML binding.
    /// </summary>
    public class Binding : IBinding
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
        /// Creates a subject that can be used to get and set the value of the binding.
        /// </summary>
        /// <param name="target">The target instance.</param>
        /// <param name="targetProperty">The target property. May be null.</param>
        /// <returns>An <see cref="ISubject{Object}"/>.</returns>
        public ISubject<object> CreateSubject(
            IPerspexObject target, 
            PerspexProperty targetProperty)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            var pathInfo = ParsePath(Path);
            ValidateState(pathInfo);

            ExpressionObserver observer;
            var targetIsDataContext = targetProperty == Control.DataContextProperty;

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
                targetProperty?.PropertyType ?? typeof(object),
                Converter ?? DefaultValueConverter.Instance,
                ConverterParameter);
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
            IPerspexObject target,
            string path,
            bool targetIsDataContext)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            if (!targetIsDataContext)
            {
                var update = target.GetObservable(Control.DataContextProperty)
                    .Skip(1)
                    .Select(_ => Unit.Default);
                var result = new ExpressionObserver(
                    () => target.GetValue(Control.DataContextProperty),
                    path,
                    update);

                return result;
            }
            else
            {
                return new ExpressionObserver(
                    target.GetObservable(Visual.VisualParentProperty)
                          .OfType<IPerspexObject>()
                          .Select(x => x.GetObservable(Control.DataContextProperty))
                          .Switch(),
                    path);
            }
        }

        private ExpressionObserver CreateTemplatedParentSubject(
            IPerspexObject target,
            string path)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            var update = target.GetObservable(Control.TemplatedParentProperty)
                .Skip(1)
                .Select(_ => Unit.Default);

            var result = new ExpressionObserver(
                () => target.GetValue(Control.TemplatedParentProperty),
                path,
                update);

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
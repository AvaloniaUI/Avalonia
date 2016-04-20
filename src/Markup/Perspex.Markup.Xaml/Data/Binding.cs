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
        /// Initializes a new instance of the <see cref="Binding"/> class.
        /// </summary>
        public Binding()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Binding"/> class.
        /// </summary>
        /// <param name="path">The binding path.</param>
        public Binding(string path)
        {
            Path = path;
        }

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
        /// Gets or sets the value to use when the binding is unable to produce a value.
        /// </summary>
        public object FallbackValue { get; set; }

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the binding path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the binding priority.
        /// </summary>
        public BindingPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the relative source for the binding.
        /// </summary>
        public RelativeSource RelativeSource { get; set; }

        /// <summary>
        /// Gets or sets the source for the binding.
        /// </summary>
        public object Source { get; set; }

        /// <summary>
        /// Gets or sets the validation methods for the binding to use.
        /// </summary>
        public ValidationMethods ValidationMethods { get; set; }

        /// <inheritdoc/>
        public InstancedBinding Initiate(
            IPerspexObject target, 
            PerspexProperty targetProperty,
            object anchor = null)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            var pathInfo = ParsePath(Path);
            ValidateState(pathInfo);

            ExpressionObserver observer;

            if (pathInfo.ElementName != null || ElementName != null)
            {
                observer = CreateElementObserver(
                    (target as IControl) ?? (anchor as IControl),
                    pathInfo.ElementName ?? ElementName, 
                    pathInfo.Path);
            }
            else if (Source != null)
            {
                observer = CreateSourceObserver(Source, pathInfo.Path);
            }
            else if (RelativeSource == null || RelativeSource.Mode == RelativeSourceMode.DataContext)
            {
                observer = CreateDataContexObserver(
                    target, 
                    pathInfo.Path,
                    targetProperty == Control.DataContextProperty,
                    anchor);
            }
            else if (RelativeSource.Mode == RelativeSourceMode.TemplatedParent)
            {
                observer = CreateTemplatedParentObserver(target, pathInfo.Path);
            }
            else
            {
                throw new NotSupportedException();
            }

            var subject = new ExpressionSubject(
                observer,
                targetProperty?.PropertyType ?? typeof(object),
                Converter ?? DefaultValueConverter.Instance,
                ConverterParameter,
                FallbackValue,
                Priority);

            return new InstancedBinding(subject, Mode, Priority);
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

        private ExpressionObserver CreateDataContexObserver(
            IPerspexObject target,
            string path,
            bool targetIsDataContext,
            object anchor)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            if (!(target is IControl))
            {
                target = anchor as IControl;

                if (target == null)
                {
                    throw new InvalidOperationException("Cannot find a DataContext to bind to.");
                }
            }

            if (!targetIsDataContext)
            {
                var update = target.GetObservable(Control.DataContextProperty)
                    .Skip(1)
                    .Select(_ => Unit.Default);
                var result = new ExpressionObserver(
                    () => target.GetValue(Control.DataContextProperty),
                    path,
                    update, ValidationMethods);

                return result;
            }
            else
            {
                return new ExpressionObserver(
                    target.GetObservable(Visual.VisualParentProperty)
                          .OfType<IPerspexObject>()
                          .Select(x => x.GetObservable(Control.DataContextProperty))
                          .Switch(),
                    path, ValidationMethods);
            }
        }

        private ExpressionObserver CreateElementObserver(IControl target, string elementName, string path)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            var result = new ExpressionObserver(
                ControlLocator.Track(target, elementName),
                path, ValidationMethods);
            return result;
        }

        private ExpressionObserver CreateSourceObserver(object source, string path)
        {
            Contract.Requires<ArgumentNullException>(source != null);

            return new ExpressionObserver(source, path, ValidationMethods);
        }

        private ExpressionObserver CreateTemplatedParentObserver(
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

        private class PathInfo
        {
            public string Path { get; set; }
            public string ElementName { get; set; }
        }
    }
}
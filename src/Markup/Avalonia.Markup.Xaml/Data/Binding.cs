// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Data;
using Avalonia.VisualTree;

namespace Avalonia.Markup.Xaml.Data
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
            FallbackValue = AvaloniaProperty.UnsetValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Binding"/> class.
        /// </summary>
        /// <param name="path">The binding path.</param>
        /// <param name="mode">The binding mode.</param>
        public Binding(string path, BindingMode mode = BindingMode.Default)
            : this()
        {
            Path = path;
            Mode = mode;
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

        /// <inheritdoc/>
        public InstancedBinding Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object anchor = null,
            bool enableDataValidation = false)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            var pathInfo = ParsePath(Path);
            ValidateState(pathInfo);
            enableDataValidation = enableDataValidation && Priority == BindingPriority.LocalValue;

            var elementName = ElementName ?? pathInfo.ElementName;
            var relativeSource = RelativeSource ?? pathInfo.RelativeSource;

            ExpressionObserver observer;

            if (elementName != null)
            {
                observer = CreateElementObserver(
                    (target as IControl) ?? (anchor as IControl),
                    elementName,
                    pathInfo.Path);
            }
            else if (Source != null)
            {
                observer = CreateSourceObserver(Source, pathInfo.Path, enableDataValidation);
            }
            else if (relativeSource == null || relativeSource.Mode == RelativeSourceMode.DataContext)
            {
                observer = CreateDataContexObserver(
                    target,
                    pathInfo.Path,
                    targetProperty == Control.DataContextProperty,
                    anchor,
                    enableDataValidation);
            }
            else if (relativeSource.Mode == RelativeSourceMode.Self)
            {
                observer = CreateSourceObserver(target, pathInfo.Path, enableDataValidation);
            }
            else if (relativeSource.Mode == RelativeSourceMode.TemplatedParent)
            {
                observer = CreateTemplatedParentObserver(target, pathInfo.Path);
            }
            else if (relativeSource.Mode == RelativeSourceMode.FindAncestor)
            {
                if (relativeSource.AncestorType == null)
                {
                    throw new InvalidOperationException("AncestorType must be set for RelativeSourceModel.FindAncestor.");
                }

                observer = CreateFindAncestorObserver(
                    (target as IControl) ?? (anchor as IControl),
                    relativeSource,
                    pathInfo.Path);
            }
            else
            {
                throw new NotSupportedException();
            }

            var fallback = FallbackValue;

            // If we're binding to DataContext and our fallback is UnsetValue then override
            // the fallback value to null, as broken bindings to DataContext must reset the
            // DataContext in order to not propagate incorrect DataContexts to child controls.
            // See Avalonia.Markup.Xaml.UnitTests.Data.DataContext_Binding_Should_Produce_Correct_Results.
            if (targetProperty == Control.DataContextProperty && fallback == AvaloniaProperty.UnsetValue)
            {
                fallback = null;
            }

            var subject = new BindingExpression(
                observer,
                targetProperty?.PropertyType ?? typeof(object),
                fallback,
                Converter ?? DefaultValueConverter.Instance,
                ConverterParameter,
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
            else if (path.StartsWith("$"))
            {
                var relativeSource = new RelativeSource();
                result.RelativeSource = relativeSource;
                var dot = path.IndexOf('.');
                string relativeSourceMode;
                if (dot != -1)
                {
                    result.Path = path.Substring(dot + 1);
                    relativeSourceMode = path.Substring(1, dot - 1);
                }
                else
                {
                    result.Path = string.Empty;
                    relativeSourceMode = path.Substring(1);
                }

                if (relativeSourceMode == "self")
                {
                    relativeSource.Mode = RelativeSourceMode.Self;
                }
                else if(relativeSourceMode == "parent")
                {
                    relativeSource.Mode = RelativeSourceMode.FindAncestor;
                    relativeSource.AncestorType = typeof(IControl);
                    relativeSource.AncestorLevel = 1;
                }
                else if(relativeSourceMode.StartsWith("parent["))
                {
                    relativeSource.Mode = RelativeSourceMode.FindAncestor;
                    var parentConfigStart = relativeSourceMode.IndexOf('[');
                    if (!relativeSourceMode.EndsWith("]"))
                    {
                        throw new InvalidOperationException("Invalid RelativeSource binding syntax. Expected matching ']' for '['.");
                    }
                    var parentConfigParams = relativeSourceMode.Substring(parentConfigStart + 1).TrimEnd(']').Split(',');
                    if (parentConfigParams.Length > 2 || parentConfigParams.Length == 0)
                    {
                        throw new InvalidOperationException("Expected either 1 or 2 parameters for RelativeSource binding syntax");
                    }
                    else if (parentConfigParams.Length == 1)
                    {
                        if (int.TryParse(parentConfigParams[0], out int level))
                        {
                            relativeSource.AncestorType = typeof(IControl);
                            relativeSource.AncestorLevel = level + 1;
                        }
                        else
                        {
                            relativeSource.AncestorType = LookupAncestorType(parentConfigParams[0]);
                        }
                    }
                    else
                    {
                        relativeSource.AncestorType = LookupAncestorType(parentConfigParams[0]);
                        relativeSource.AncestorLevel = int.Parse(parentConfigParams[1]) + 1;
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Invalid RelativeSource binding syntax: {relativeSourceMode}");
                }
            }
            else
            {
                result.Path = path;
            }

            return result;
        }

        private static Type LookupAncestorType(string ancestorTypeName)
        {
            //TODO: What is our syntax for type lookup here?
            throw new NotImplementedException();
        }

        private void ValidateState(PathInfo pathInfo)
        {
            if (pathInfo.ElementName != null && ElementName != null)
            {
                throw new InvalidOperationException(
                    "ElementName property cannot be set when an #elementName path is provided.");
            }

            if (pathInfo.RelativeSource != null && RelativeSource != null)
            {
                throw new InvalidOperationException(
                    "ElementName property cannot be set when a $self or $parent path is provided.");
            }

            if ((pathInfo.ElementName != null || ElementName != null) &&
                (pathInfo.RelativeSource != null || RelativeSource != null))
            {
                throw new InvalidOperationException(
                    "ElementName property cannot be set with a RelativeSource.");
            }
        }

        private ExpressionObserver CreateDataContexObserver(
            IAvaloniaObject target,
            string path,
            bool targetIsDataContext,
            object anchor,
            bool enableDataValidation)
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
                    update,
                    enableDataValidation);

                return result;
            }
            else
            {
                return new ExpressionObserver(
                    GetParentDataContext(target),
                    path,
                    enableDataValidation);
            }
        }

        private ExpressionObserver CreateElementObserver(IControl target, string elementName, string path)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            var description = $"#{elementName}.{path}";
            var result = new ExpressionObserver(
                ControlLocator.Track(target, elementName),
                path,
                false,
                description);
            return result;
        }

        private ExpressionObserver CreateFindAncestorObserver(
            IControl target,
            RelativeSource relativeSource,
            string path)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            return new ExpressionObserver(
                ControlLocator.Track(target, relativeSource.AncestorType, relativeSource.AncestorLevel -1),
                path);
        }

        private ExpressionObserver CreateSourceObserver(
            object source,
            string path,
            bool enabledDataValidation)
        {
            Contract.Requires<ArgumentNullException>(source != null);

            return new ExpressionObserver(source, path, enabledDataValidation);
        }

        private ExpressionObserver CreateTemplatedParentObserver(
            IAvaloniaObject target,
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

        private IObservable<object> GetParentDataContext(IAvaloniaObject target)
        {
            // The DataContext is based on the visual parent and not the logical parent: this may
            // seem unintuitive considering the fact that property inheritance works on the logical
            // tree, but consider a ContentControl with a ContentPresenter. The ContentControl's
            // Content property is bound to a value which becomes the ContentPresenter's 
            // DataContext - it is from this that the child hosted by the ContentPresenter needs to
            // inherit its DataContext.
            return target.GetObservable(Visual.VisualParentProperty)
                .Select(x =>
                {
                    return (x as IAvaloniaObject)?.GetObservable(Control.DataContextProperty) ?? 
                           Observable.Return((object)null);
                }).Switch();
        }

        private class PathInfo
        {
            public string Path { get; set; }
            public string ElementName { get; set; }
            public RelativeSource RelativeSource { get; set; }
        }
    }
}
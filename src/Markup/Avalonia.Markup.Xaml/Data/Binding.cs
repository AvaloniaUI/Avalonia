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
        public string Path { get; set; } = "";

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

        internal WeakReference DefaultAnchor { get; set; }

        /// <inheritdoc/>
        public InstancedBinding Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object anchor = null,
            bool enableDataValidation = false)
        {
            Contract.Requires<ArgumentNullException>(target != null);
            anchor = anchor ?? DefaultAnchor?.Target;
            
            enableDataValidation = enableDataValidation && Priority == BindingPriority.LocalValue;
            
            ExpressionObserver observer;

            if (ElementName != null)
            {
                observer = CreateElementObserver(
                    (target as IControl) ?? (anchor as IControl),
                    ElementName,
                    Path,
                    enableDataValidation);
            }
            else if (Source != null)
            {
                observer = CreateSourceObserver(Source, Path, enableDataValidation);
            }
            else if (RelativeSource == null || RelativeSource.Mode == RelativeSourceMode.DataContext)
            {
                observer = CreateDataContexObserver(
                    target,
                    Path,
                    targetProperty == Control.DataContextProperty,
                    anchor,
                    enableDataValidation);
            }
            else if (RelativeSource.Mode == RelativeSourceMode.Self)
            {
                observer = CreateSourceObserver(target, Path, enableDataValidation);
            }
            else if (RelativeSource.Mode == RelativeSourceMode.TemplatedParent)
            {
                observer = CreateTemplatedParentObserver(target, Path, enableDataValidation);
            }
            else if (RelativeSource.Mode == RelativeSourceMode.FindAncestor)
            {
                if (RelativeSource.Tree == TreeType.Visual && RelativeSource.AncestorType == null)
                {
                    throw new InvalidOperationException("AncestorType must be set for RelativeSourceMode.FindAncestor when searching the visual tree.");
                }

                observer = CreateFindAncestorObserver(
                    (target as IControl) ?? (anchor as IControl),
                    RelativeSource,
                    Path,
                    enableDataValidation);
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

        private ExpressionObserver CreateElementObserver(
            IControl target,
            string elementName,
            string path,
            bool enableDataValidation)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            var description = $"#{elementName}.{path}";
            var result = new ExpressionObserver(
                ControlLocator.Track(target, elementName),
                path,
                enableDataValidation,
                description);
            return result;
        }

        private ExpressionObserver CreateFindAncestorObserver(
            IControl target,
            RelativeSource relativeSource,
            string path,
            bool enableDataValidation)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            return new ExpressionObserver(
                ControlLocator.Track(target, relativeSource.Tree, relativeSource.AncestorLevel - 1, relativeSource.AncestorType),
                path,
                enableDataValidation);
        }

        private ExpressionObserver CreateSourceObserver(
            object source,
            string path,
            bool enableDataValidation)
        {
            Contract.Requires<ArgumentNullException>(source != null);

            return new ExpressionObserver(source, path, enableDataValidation);
        }

        private ExpressionObserver CreateTemplatedParentObserver(
            IAvaloniaObject target,
            string path,
            bool enableDataValidation)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            var update = target.GetObservable(Control.TemplatedParentProperty)
                .Skip(1)
                .Select(_ => Unit.Default);

            var result = new ExpressionObserver(
                () => target.GetValue(Control.TemplatedParentProperty),
                path,
                update,
                enableDataValidation);

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

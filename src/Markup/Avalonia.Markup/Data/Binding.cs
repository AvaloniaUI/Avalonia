// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.LogicalTree;
using Avalonia.Markup.Parsers;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Data
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

        /// <summary>
        /// Gets or sets the string format.
        /// </summary>
        public string StringFormat { get; set; }

        public WeakReference DefaultAnchor { get; set; }
        
        public WeakReference<INameScope> NameScope { get; set; }

        /// <summary>
        /// Gets or sets a function used to resolve types from names in the binding path.
        /// </summary>
        public Func<string, string, Type> TypeResolver { get; set; }

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

            INameScope nameScope = null;
            NameScope?.TryGetTarget(out nameScope);
            var (node, mode) = ExpressionObserverBuilder.Parse(Path, enableDataValidation, TypeResolver, nameScope);

            if (ElementName != null)
            {
                observer = CreateElementObserver(
                    (target as IStyledElement) ?? (anchor as IStyledElement),
                    ElementName,
                    node);
            }
            else if (Source != null)
            {
                observer = CreateSourceObserver(Source, node);
            }
            else if (RelativeSource == null)
            {
                if (mode == SourceMode.Data)
                {
                    observer = CreateDataContextObserver(
                        target,
                        node,
                        targetProperty == StyledElement.DataContextProperty,
                        anchor); 
                }
                else
                {
                    observer = new ExpressionObserver(
                        (target as IStyledElement) ?? (anchor as IStyledElement),
                        node);
                }
            }
            else if (RelativeSource.Mode == RelativeSourceMode.DataContext)
            {
                observer = CreateDataContextObserver(
                    target,
                    node,
                    targetProperty == StyledElement.DataContextProperty,
                    anchor);
            }
            else if (RelativeSource.Mode == RelativeSourceMode.Self)
            {
                observer = CreateSourceObserver(
                    (target as IStyledElement) ?? (anchor as IStyledElement),
                    node);
            }
            else if (RelativeSource.Mode == RelativeSourceMode.TemplatedParent)
            {
                observer = CreateTemplatedParentObserver(
                    (target as IStyledElement) ?? (anchor as IStyledElement),
                    node);
            }
            else if (RelativeSource.Mode == RelativeSourceMode.FindAncestor)
            {
                if (RelativeSource.Tree == TreeType.Visual && RelativeSource.AncestorType == null)
                {
                    throw new InvalidOperationException("AncestorType must be set for RelativeSourceMode.FindAncestor when searching the visual tree.");
                }

                observer = CreateFindAncestorObserver(
                    (target as IStyledElement) ?? (anchor as IStyledElement),
                    RelativeSource,
                    node);
            }
            else
            {
                throw new NotSupportedException();
            }

            var fallback = FallbackValue;

            // If we're binding to DataContext and our fallback is UnsetValue then override
            // the fallback value to null, as broken bindings to DataContext must reset the
            // DataContext in order to not propagate incorrect DataContexts to child controls.
            // See Avalonia.Markup.UnitTests.Data.DataContext_Binding_Should_Produce_Correct_Results.
            if (targetProperty == StyledElement.DataContextProperty && fallback == AvaloniaProperty.UnsetValue)
            {
                fallback = null;
            }

            var converter = Converter;
            var targetType = targetProperty?.PropertyType ?? typeof(object);

            // We only respect `StringFormat` if the type of the property we're assigning to will
            // accept a string. Note that this is slightly different to WPF in that WPF only applies
            // `StringFormat` for target type `string` (not `object`).
            if (!string.IsNullOrWhiteSpace(StringFormat) && 
                (targetType == typeof(string) || targetType == typeof(object)))
            {
                converter = new StringFormatValueConverter(StringFormat, converter);
            }

            var subject = new BindingExpression(
                observer,
                targetType,
                fallback,
                converter ?? DefaultValueConverter.Instance,
                ConverterParameter,
                Priority);

            return new InstancedBinding(subject, Mode, Priority);
        }

        private ExpressionObserver CreateDataContextObserver(
            IAvaloniaObject target,
            ExpressionNode node,
            bool targetIsDataContext,
            object anchor)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            if (!(target is IStyledElement))
            {
                target = anchor as IStyledElement;

                if (target == null)
                {
                    throw new InvalidOperationException("Cannot find a DataContext to bind to.");
                }
            }

            if (!targetIsDataContext)
            {
                var result = new ExpressionObserver(
                    () => target.GetValue(StyledElement.DataContextProperty),
                    node,
                    new UpdateSignal(target, StyledElement.DataContextProperty),
                    null);

                return result;
            }
            else
            {
                return new ExpressionObserver(
                    GetParentDataContext(target),
                    node,
                    null);
            }
        }

        private ExpressionObserver CreateElementObserver(
            IStyledElement target,
            string elementName,
            ExpressionNode node)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            NameScope.TryGetTarget(out var scope);
            if (scope == null)
                throw new InvalidOperationException("Name scope is null or was already collected");
            var result = new ExpressionObserver(
                NameScopeLocator.Track(scope, elementName),
                node,
                null);
            return result;
        }

        private ExpressionObserver CreateFindAncestorObserver(
            IStyledElement target,
            RelativeSource relativeSource,
            ExpressionNode node)
        {
            Contract.Requires<ArgumentNullException>(target != null);

            IObservable<object> controlLocator;

            switch (relativeSource.Tree)
            {
                case TreeType.Logical:
                    controlLocator = ControlLocator.Track(
                        (ILogical)target,
                        relativeSource.AncestorLevel - 1,
                        relativeSource.AncestorType);
                    break;
                case TreeType.Visual:
                    controlLocator = VisualLocator.Track(
                        (IVisual)target,
                        relativeSource.AncestorLevel - 1,
                        relativeSource.AncestorType);
                    break;
                default:
                    throw new InvalidOperationException("Invalid tree to traverse.");
            }

            return new ExpressionObserver(
                controlLocator,
                node,
                null);
        }

        private ExpressionObserver CreateSourceObserver(
            object source,
            ExpressionNode node)
        {
            Contract.Requires<ArgumentNullException>(source != null);

            return new ExpressionObserver(source, node);
        }

        private ExpressionObserver CreateTemplatedParentObserver(
            IAvaloniaObject target,
            ExpressionNode node)
        {
            Contract.Requires<ArgumentNullException>(target != null);
            
            var result = new ExpressionObserver(
                () => target.GetValue(StyledElement.TemplatedParentProperty),
                node,
                new UpdateSignal(target, StyledElement.TemplatedParentProperty),
                null);

            return result;
        }

        private IObservable<object> GetParentDataContext(IAvaloniaObject target)
        {
            // The DataContext is based on the visual parent and not the logical parent: this may
            // seem counter intuitive considering the fact that property inheritance works on the logical
            // tree, but consider a ContentControl with a ContentPresenter. The ContentControl's
            // Content property is bound to a value which becomes the ContentPresenter's 
            // DataContext - it is from this that the child hosted by the ContentPresenter needs to
            // inherit its DataContext.
            return target.GetObservable(Visual.VisualParentProperty)
                .Select(x =>
                {
                    return (x as IAvaloniaObject)?.GetObservable(StyledElement.DataContextProperty) ?? 
                           Observable.Return((object)null);
                }).Switch();
        }

        private class UpdateSignal : SingleSubscriberObservableBase<Unit>
        {
            private readonly IAvaloniaObject _target;
            private readonly AvaloniaProperty _property;

            public UpdateSignal(IAvaloniaObject target, AvaloniaProperty property)
            {
                _target = target;
                _property = property;
            }

            protected override void Subscribed()
            {
                _target.PropertyChanged += PropertyChanged;
            }

            protected override void Unsubscribed()
            {
                _target.PropertyChanged -= PropertyChanged;
            }

            private void PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
            {
                if (e.Property == _property)
                {
                    PublishNext(Unit.Default);
                }
            }
        }
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.LogicalTree;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Data
{
    public abstract class BindingBase : IBinding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Binding"/> class.
        /// </summary>
        public BindingBase()
        {
            FallbackValue = AvaloniaProperty.UnsetValue;
            TargetNullValue = AvaloniaProperty.UnsetValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Binding"/> class.
        /// </summary>
        /// <param name="mode">The binding mode.</param>
        public BindingBase(BindingMode mode = BindingMode.Default)
            :this()
        {
            Mode = mode;
        }

        /// <summary>
        /// Gets or sets the <see cref="IValueConverter"/> to use.
        /// </summary>
        public IValueConverter? Converter { get; set; }

        /// <summary>
        /// Gets or sets a parameter to pass to <see cref="Converter"/>.
        /// </summary>
        public object? ConverterParameter { get; set; }

        /// <summary>
        /// Gets or sets the value to use when the binding is unable to produce a value.
        /// </summary>
        public object? FallbackValue { get; set; }

        /// <summary>
        /// Gets or sets the value to use when the binding result is null.
        /// </summary>
        public object? TargetNullValue { get; set; }

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the binding priority.
        /// </summary>
        public BindingPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the string format.
        /// </summary>
        public string? StringFormat { get; set; }

        public WeakReference? DefaultAnchor { get; set; }

        public WeakReference<INameScope?>? NameScope { get; set; }

        private protected abstract ExpressionObserver CreateExpressionObserver(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor,
            bool enableDataValidation);

        /// <inheritdoc/>
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = TrimmingMessages.TypeConversionSupressWarningMessage)]
        public InstancedBinding? Initiate(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor = null,
            bool enableDataValidation = false)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));

            anchor = anchor ?? DefaultAnchor?.Target;

            enableDataValidation = enableDataValidation && Priority == BindingPriority.LocalValue;

            var observer = CreateExpressionObserver(target, targetProperty, anchor, enableDataValidation);

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
                converter = new StringFormatValueConverter(StringFormat!, converter);
            }

            var subject = new BindingExpression(
                observer,
                targetType,
                fallback,
                TargetNullValue,
                converter ?? DefaultValueConverter.Instance,
                ConverterParameter,
                Priority);

            return new InstancedBinding(subject, Mode, Priority);
        }

        private protected ExpressionObserver CreateDataContextObserver(
            AvaloniaObject target,
            ExpressionNode node,
            bool targetIsDataContext,
            object? anchor)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));

            if (target is not IDataContextProvider)
            {
                if (anchor is IDataContextProvider && anchor is AvaloniaObject ao)
                    target = ao;
                else
                    throw new InvalidOperationException("Cannot find a DataContext to bind to.");
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

        private protected ExpressionObserver CreateElementObserver(
            StyledElement target,
            string elementName,
            ExpressionNode node)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));

            if (NameScope is null || !NameScope.TryGetTarget(out var scope))
                throw new InvalidOperationException("Name scope is null or was already collected");
            var result = new ExpressionObserver(
                NameScopeLocator.Track(scope, elementName),
                node,
                null);
            return result;
        }

        private protected ExpressionObserver CreateFindAncestorObserver(
            StyledElement target,
            RelativeSource relativeSource,
            ExpressionNode node)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));

            IObservable<object?> controlLocator;

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
                        (Visual)target,
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

        private protected ExpressionObserver CreateSourceObserver(
            object source,
            ExpressionNode node)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));

            return new ExpressionObserver(source, node);
        }

        private protected ExpressionObserver CreateTemplatedParentObserver(
            AvaloniaObject target,
            ExpressionNode node)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));

            var result = new ExpressionObserver(
                () => target.GetValue(StyledElement.TemplatedParentProperty),
                node,
                new UpdateSignal(target, StyledElement.TemplatedParentProperty),
                null);

            return result;
        }

        private IObservable<object?> GetParentDataContext(AvaloniaObject target)
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
                    return (x as AvaloniaObject)?.GetObservable(StyledElement.DataContextProperty) ??
                           Observable.Return((object?)null);
                }).Switch();
        }

        private class UpdateSignal : SingleSubscriberObservableBase<ValueTuple>
        {
            private readonly AvaloniaObject _target;
            private readonly AvaloniaProperty _property;

            public UpdateSignal(AvaloniaObject target, AvaloniaProperty property)
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

            private void PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
            {
                if (e.Property == _property)
                {
                    PublishNext(default);
                }
            }
        }
    }
}

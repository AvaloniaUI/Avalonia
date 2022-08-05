using System;
using System.Reactive.Disposables;
using Avalonia.Data.Core;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Data
{
    /// <summary>
    /// A binding whose input and output are strongly-typed.
    /// </summary>
    /// <typeparam name="TIn">The binding input.</typeparam>
    /// <typeparam name="TOut">The binding output.</typeparam>
    /// <remarks>
    /// <see cref="TypedBinding{TIn, TOut}"/> represents a strongly-typed binding as opposed to
    /// Binding which boxes value types. It is represented as a read and write delegate plus a
    /// collection of triggers:
    /// 
    /// - <see cref="Read"/> reads the value given a binding input
    /// - <see cref="Write"/> writes a value given a binding input
    /// - <see cref="ReadTriggers"/> holds a collection of objects which listen for changes in each
    ///   object traversed by <see cref="Read"/>. For example if Read is implemented as 
    ///   `x => x.Foo.Bar.Baz` then there would be three triggers: `x => x.Foo`, `x => x.Foo.Bar`
    ///   and `x => x.Foo.Bar.Baz`.
    ///   
    /// This class represents a binding which has not been instantiated on an object. When the
    /// <see cref="Bind(IAvaloniaObject, DirectPropertyBase{TOut})"/> or
    /// <see cref="Bind(IAvaloniaObject, StyledPropertyBase{TOut})"/> methods are called, then
    /// an instance of <see cref="TypedBindingExpression{TIn, TOut}"/> is created which represents
    /// the binding instantiated on that object.
    /// </remarks>
    public class TypedBinding<TIn, TOut>
        where TIn : class
    {
        /// <summary>
        /// Gets or sets the read function.
        /// </summary>
        internal Func<TIn, TOut>? Read { get; set; }

        /// <summary>
        /// Gets or sets the write function.
        /// </summary>
        internal Action<TIn, TOut>? Write { get; set; }

        /// <summary>
        /// Gets or sets the read triggers.
        /// </summary>
        internal TypedBindingTrigger<TIn>[]? ReadTriggers { get; set; }

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        internal BindingMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the binding priority.
        /// </summary>
        internal BindingPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the value to use when the binding is unable to produce a value.
        /// </summary>
        internal Optional<TOut> FallbackValue { get; set; }

        /// <summary>
        /// Gets or sets the source for the binding.
        /// </summary>
        /// <remarks>
        /// If unset the source is the target control's <see cref="StyledElement.DataContext"/> property.
        /// </remarks>
        internal Optional<TIn> Source { get; set; }

        /// <summary>
        /// Creates a binding to the specified styled property.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="property">The target property.</param>
        /// <returns>A disposable that can be used to terminate the binding.</returns>
        public IDisposable Bind(IAvaloniaObject target, StyledPropertyBase<TOut> property)
        {
            var mode = GetMode(target, property);
            var expression = CreateExpression(target, property, mode);

            switch (mode)
            {
                case BindingMode.Default:
                case BindingMode.OneWay:
                    return target.Bind(property, expression, Priority);
                case BindingMode.TwoWay:
                    return new CompositeDisposable(
                        target.Bind(property, expression, Priority),
                        target.GetBindingObservable(property).Subscribe(expression));
                default:
                    throw new ArgumentException("Invalid binding mode.");
            }
        }

        /// <summary>
        /// Creates a binding to the specified direct property.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="property">The target property.</param>
        /// <returns>A disposable that can be used to terminate the binding.</returns>
        public IDisposable Bind(IAvaloniaObject target, DirectPropertyBase<TOut> property)
        {
            var mode = GetMode(target, property);
            var expression = CreateExpression(target, property, mode);

            switch (mode)
            {
                case BindingMode.Default:
                case BindingMode.OneWay:
                    return target.Bind(property, expression, Priority);
                case BindingMode.TwoWay:
                    return new CompositeDisposable(
                        target.GetBindingObservable(property).Subscribe(expression),
                        target.Bind(property, expression, Priority));
                case BindingMode.OneTime:
                    return target.Bind(
                        property,
                        System.Reactive.Linq.Observable.TakeUntil(expression, x => x.HasValue),
                        Priority);
                case BindingMode.OneWayToSource:
                    return new CompositeDisposable(
                        target.Bind(property, expression, Priority),
                        target.GetBindingObservable(property).Subscribe(expression));
                default:
                    throw new ArgumentException("Invalid binding mode.");
            }
        }

        /// <summary>
        /// Instances the binding on a data source.
        /// </summary>
        /// <param name="source">The data source.</param>
        /// <param name="mode">The binding mode.</param>
        public TypedBindingExpression<TIn, TOut> Instance(TIn? source, BindingMode mode = BindingMode.OneWay)
        {
            return Instance(ObservableEx.SingleValue(source), mode, FallbackValue);
        }

        private TypedBindingExpression<TIn, TOut> CreateExpression(
            IAvaloniaObject target,
            AvaloniaProperty property,
            BindingMode mode)
        {
            var targetIsDataContext = property == StyledElement.DataContextProperty;
            var root = GetRoot(target, property);
            var fallback = FallbackValue;

            // If we're binding to DataContext and our fallback is unset then override the fallback
            // value to null, as broken bindings to DataContext must reset the DataContext in order
            // to not propagate incorrect DataContexts to child controls. See 
            // TypedBinding.DataContext_Binding_Should_Produce_Correct_Results.
            if (targetIsDataContext && !fallback.HasValue)
            {
                fallback = new Optional<TOut>(default!);
            }

            return Instance(root, mode, fallback);
        }

        private TypedBindingExpression<TIn, TOut> Instance(IObservable<TIn?> source, BindingMode mode, Optional<TOut> fallback)
        {
            if (mode != BindingMode.OneWayToSource)
            {
                _ = Read ?? throw new InvalidOperationException("Cannot bind TypedBinding: Read is uninitialized.");
                _ = ReadTriggers ?? throw new InvalidOperationException("Cannot bind TypedBinding: Links is uninitialized.");
            }

            if ((mode == BindingMode.TwoWay || mode == BindingMode.OneWayToSource) && Write is null)
                throw new InvalidOperationException($"Cannot bind TypedBinding {Mode}: Write is uninitialized.");
            
            return new TypedBindingExpression<TIn, TOut>(source, Read, Write, ReadTriggers, fallback);
        }

        private BindingMode GetMode(IAvaloniaObject target, AvaloniaProperty property)
        {
            return Mode == BindingMode.Default ? property.GetMetadata(target.GetType()).DefaultBindingMode : Mode;
        }

        private IObservable<TIn?> GetRoot(IAvaloniaObject target, AvaloniaProperty property)
        {
            if (Source.HasValue)
            {
                return ObservableEx.SingleValue(Source.Value);
            }
            else if (property == StyledElement.DataContextProperty)
            {
                return new ParentDataContextRoot<TIn>((IVisual)target);
            }
            else
            {
                return new DataContextRoot<TIn>((IStyledElement)target);
            }
        }
    }
}

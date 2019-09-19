using System;
using Avalonia.Data.Core;
using Avalonia.VisualTree;

namespace Avalonia.Data
{
    public class CompiledBinding<TIn, TOut> : IBinding
        where TIn : class
    {
        /// <summary>
        /// Gets or sets the read function.
        /// </summary>
        public Func<TIn, TOut> Read { get; set; }

        /// <summary>
        /// Gets or sets the write function.
        /// </summary>
        public Action<TIn, TOut> Write { get; set; }

        /// <summary>
        /// Gets or sets the links in the binding chain.
        /// </summary>
        public Func<TIn, object>[] Links { get; set; }

        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the binding priority.
        /// </summary>
        public BindingPriority Priority { get; set; }

        /// <summary>
        /// Gets or sets the value to use when the binding is unable to produce a value.
        /// </summary>
        public FallbackValue<TOut> FallbackValue { get; set; }

        public InstancedBinding Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object anchor = null,
            bool enableDataValidation = false)
        {
            var targetIsDataContext = targetProperty == StyledElement.DataContextProperty;
            var root = targetIsDataContext ?
                (IObservable<TIn>)new ParentDataContextRoot<TIn>((IVisual)target) :
                new DataContextRoot<TIn>((IStyledElement)target);
            var fallback = FallbackValue;

            // If we're binding to DataContext and our fallback is unset then override the fallback
            // value to null, as broken bindings to DataContext must reset the DataContext in order
            // to not propagate incorrect DataContexts to child controls. See 
            // CompiledBindingTests.DataContext_Binding_Should_Produce_Correct_Results.
            if (targetIsDataContext && !fallback.HasValue)
            {
                fallback = new FallbackValue<TOut>(default);
            }

            var expression = new CompiledBindingExpression<TIn, TOut>(root, Read, Write, Links, fallback);
            var adapter = new CompiledBindingExpressionAdapter<TOut>(expression, enableDataValidation);
            return new InstancedBinding(adapter, Mode, Priority);
        }
    }
}

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

        public InstancedBinding Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object anchor = null,
            bool enableDataValidation = false)
        {
            var root = targetProperty == StyledElement.DataContextProperty ?
                (IObservable<TIn>)new ParentDataContextRoot<TIn>((IVisual)target) :
                new DataContextRoot<TIn>((IStyledElement)target);
            var expression = new CompiledBindingExpression<TIn, TOut>(root, Read, Write, Links);
            var adapter = new CompiledBindingExpressionAdapter<TOut>(expression, enableDataValidation);
            return new InstancedBinding(adapter, Mode, Priority);
        }
    }
}

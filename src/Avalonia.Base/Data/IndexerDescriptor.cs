using System;

namespace Avalonia.Data
{
    /// <summary>
    /// Holds a description of a binding for <see cref="AvaloniaObject"/>'s [] operator.
    /// </summary>
    public class IndexerDescriptor : IObservable<object?>, IDescription
    {
        /// <summary>
        /// Gets or sets the binding mode.
        /// </summary>
        public BindingMode Mode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the binding priority.
        /// </summary>
        public BindingPriority Priority
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the source property.
        /// </summary>
        public AvaloniaProperty? Property
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the source object.
        /// </summary>
        public AvaloniaObject? Source
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the source observable.
        /// </summary>
        /// <remarks>
        /// If null, then <see cref="Source"/>.<see cref="Property"/> will be used.
        /// </remarks>
        public IObservable<object>? SourceObservable
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a description of the binding.
        /// </summary>
        public string Description => $"{Source?.GetType().Name}.{Property?.Name}";

        /// <summary>
        /// Makes a two-way binding.
        /// </summary>
        /// <param name="binding">The current binding.</param>
        /// <returns>A two-way binding.</returns>
        public static IndexerDescriptor operator !(IndexerDescriptor binding)
        {
            return binding.WithMode(BindingMode.TwoWay);
        }

        /// <summary>
        /// Makes a two-way binding.
        /// </summary>
        /// <param name="binding">The current binding.</param>
        /// <returns>A two-way binding.</returns>
        public static IndexerDescriptor operator ~(IndexerDescriptor binding)
        {
            return binding.WithMode(BindingMode.TwoWay);
        }

        /// <summary>
        /// Modifies the binding mode.
        /// </summary>
        /// <param name="mode">The binding mode.</param>
        /// <returns>The object that the method was called on.</returns>
        public IndexerDescriptor WithMode(BindingMode mode)
        {
            Mode = mode;
            return this;
        }

        /// <summary>
        /// Modifies the binding priority.
        /// </summary>
        /// <param name="priority">The binding priority.</param>
        /// <returns>The object that the method was called on.</returns>
        public IndexerDescriptor WithPriority(BindingPriority priority)
        {
            Priority = priority;
            return this;
        }

        /// <inheritdoc/>
        public IDisposable Subscribe(IObserver<object?> observer)
        {
            if (SourceObservable is null && Source is null)
                throw new InvalidOperationException("Cannot subscribe to IndexerDescriptor.");
            if (Property is null)
                throw new InvalidOperationException("Cannot subscribe to IndexerDescriptor.");

            return (SourceObservable ?? Source!.GetObservable(Property)).Subscribe(observer);
        }
    }
}

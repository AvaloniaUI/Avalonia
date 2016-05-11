// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive;

namespace Avalonia.Data
{
    /// <summary>
    /// Holds a description of a binding for <see cref="AvaloniaObject"/>'s [] operator.
    /// </summary>
    public class IndexerDescriptor : ObservableBase<object>, IDescription
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
        public AvaloniaProperty Property
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the source object.
        /// </summary>
        public AvaloniaObject Source
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
        public IObservable<object> SourceObservable
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a description of the binding.
        /// </summary>
        public string Description => $"{Source?.GetType().Name}.{Property.Name}";

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
        protected override IDisposable SubscribeCore(IObserver<object> observer)
        {
            return (SourceObservable ?? Source.GetObservable(Property)).Subscribe(observer);
        }
    }
}

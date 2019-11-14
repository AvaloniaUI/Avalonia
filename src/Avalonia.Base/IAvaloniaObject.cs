// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// Interface for getting/setting <see cref="AvaloniaProperty"/> values on an object.
    /// </summary>
    public interface IAvaloniaObject
    {
        /// <summary>
        /// Raised when a <see cref="AvaloniaProperty"/> value changes on this object.
        /// </summary>
        event EventHandler<AvaloniaPropertyChangedEventArgs> PropertyChanged;

        /// <summary>
        /// Clears a <see cref="AvaloniaProperty"/>'s local value.
        /// </summary>
        /// <param name="property">The property.</param>
        public void ClearValue(AvaloniaProperty property);

        /// <summary>
        /// Clears a <see cref="AvaloniaProperty"/>'s local value.
        /// </summary>
        /// <param name="property">The property.</param>
        public void ClearValue<T>(AvaloniaProperty<T> property);

        /// <summary>
        /// Gets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        object GetValue(AvaloniaProperty property);

        /// <summary>
        /// Gets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>The value.</returns>
        T GetValue<T>(AvaloniaProperty<T> property);

        /// <summary>
        /// Checks whether a <see cref="AvaloniaProperty"/> is animating.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is animating, otherwise false.</returns>
        bool IsAnimating(AvaloniaProperty property);

        /// <summary>
        /// Checks whether a <see cref="AvaloniaProperty"/> is set on this object.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>True if the property is set, otherwise false.</returns>
        bool IsSet(AvaloniaProperty property);

        /// <summary>
        /// Sets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority of the value.</param>
        void SetValue(
            AvaloniaProperty property, 
            object value, 
            BindingPriority priority = BindingPriority.LocalValue);

        /// <summary>
        /// Sets a <see cref="AvaloniaProperty"/> value.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        /// <param name="priority">The priority of the value.</param>
        void SetValue<T>(
            AvaloniaProperty<T> property,
            T value,
            BindingPriority priority = BindingPriority.LocalValue);

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        IDisposable Bind(
            AvaloniaProperty property,
            IObservable<BindingValue<object>> source,
            BindingPriority priority = BindingPriority.LocalValue);

        /// <summary>
        /// Binds a <see cref="AvaloniaProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        IDisposable Bind<T>(
            AvaloniaProperty<T> property,
            IObservable<BindingValue<T>> source,
            BindingPriority priority = BindingPriority.LocalValue);

        /// <summary>
        /// Registers an object as an inheritance child.
        /// </summary>
        /// <param name="child">The inheritance child.</param>
        /// <remarks>
        /// Inheritance children will recieve a call to
        /// <see cref="InheritedPropertyChanged{T}(AvaloniaProperty{T}, Optional{T}, Optional{T})"/>
        /// when an inheritable property value changes on the parent.
        /// </remarks>
        void AddInheritanceChild(IAvaloniaObject child);

        /// <summary>
        /// Unregisters an object as an inheritance child.
        /// </summary>
        /// <param name="child">The inheritance child.</param>
        /// <remarks>
        /// Removes an inheritance child that was added by a call to
        /// <see cref="AddInheritanceChild(IAvaloniaObject)"/>.
        /// </remarks>
        void RemoveInheritanceChild(IAvaloniaObject child);

        //void InheritanceParentChanged<T>(
        //    StyledPropertyBase<T> property,
        //    IAvaloniaObject oldParent,
        //    IAvaloniaObject newParent);

        /// <summary>
        /// Called when an inheritable property changes on an object registered as an inheritance
        /// parent.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="property">The property that has changed.</param>
        /// <param name="oldValue">The old property value.</param>
        /// <param name="newValue">The new property value.</param>
        void InheritedPropertyChanged<T>(
            AvaloniaProperty<T> property,
            Optional<T> oldValue,
            Optional<T> newValue);
    }
}

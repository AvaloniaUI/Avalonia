// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.LogicalTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Implements a name scope.
    /// </summary>
    public class NameScope : INameScope
    {
        /// <summary>
        /// Defines the NameScope attached property.
        /// </summary>
        public static readonly AttachedProperty<INameScope> NameScopeProperty =
            AvaloniaProperty.RegisterAttached<NameScope, StyledElement, INameScope>("NameScope");

        private readonly Dictionary<string, object> _inner = new Dictionary<string, object>();

        /// <summary>
        /// Raised when an element is registered with the name scope.
        /// </summary>
        public event EventHandler<NameScopeEventArgs> Registered;

        /// <summary>
        /// Raised when an element is unregistered with the name scope.
        /// </summary>
        public event EventHandler<NameScopeEventArgs> Unregistered;

        /// <summary>
        /// Finds the containing name scope for a styled element.
        /// </summary>
        /// <param name="styled">The styled element.</param>
        /// <returns>The containing name scope.</returns>
        public static INameScope FindNameScope(StyledElement styled)
        {
            Contract.Requires<ArgumentNullException>(styled != null);

            INameScope result;

            while (styled != null)
            {
                result = styled as INameScope ?? GetNameScope(styled);

                if (result != null)
                {
                    return result;
                }

                styled = (styled as ILogical)?.LogicalParent as StyledElement;
            }

            return null;
        }

        /// <summary>
        /// Gets the value of the attached <see cref="NameScopeProperty"/> on a styled element.
        /// </summary>
        /// <param name="styled">The styled element.</param>
        /// <returns>The value of the NameScope attached property.</returns>
        public static INameScope GetNameScope(StyledElement styled)
        {
            Contract.Requires<ArgumentNullException>(styled != null);

            return styled.GetValue(NameScopeProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="NameScopeProperty"/> on a styled element.
        /// </summary>
        /// <param name="styled">The styled element.</param>
        /// <param name="value">The value to set.</param>
        public static void SetNameScope(StyledElement styled, INameScope value)
        {
            Contract.Requires<ArgumentNullException>(styled != null);

            styled.SetValue(NameScopeProperty, value);
        }

        /// <summary>
        /// Registers an element with the name scope.
        /// </summary>
        /// <param name="name">The element name.</param>
        /// <param name="element">The element.</param>
        public void Register(string name, object element)
        {
            Contract.Requires<ArgumentNullException>(name != null);
            Contract.Requires<ArgumentNullException>(element != null);

            object existing;

            if (_inner.TryGetValue(name, out existing))
            {
                if (existing != element)
                {
                    throw new ArgumentException($"Control with the name '{name}' already registered.");
                }
            }
            else
            {
                _inner.Add(name, element);
                Registered?.Invoke(this, new NameScopeEventArgs(name, element));
            }
        }

        /// <summary>
        /// Finds a named element in the name scope.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The element, or null if the name was not found.</returns>
        public object Find(string name)
        {
            Contract.Requires<ArgumentNullException>(name != null);

            object result;
            _inner.TryGetValue(name, out result);
            return result;
        }

        /// <summary>
        /// Unregisters an element with the name scope.
        /// </summary>
        /// <param name="name">The name.</param>
        public void Unregister(string name)
        {
            Contract.Requires<ArgumentNullException>(name != null);

            object element;

            if (_inner.TryGetValue(name, out element))
            {
                _inner.Remove(name);
                Unregistered?.Invoke(this, new NameScopeEventArgs(name, element));
            }
        }
    }
}

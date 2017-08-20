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
            AvaloniaProperty.RegisterAttached<NameScope, Visual, INameScope>("NameScope");

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
        /// Finds the containing name scope for a visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The containing name scope.</returns>
        public static INameScope FindNameScope(Visual visual)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            INameScope result;

            while (visual != null)
            {
                result = visual as INameScope ?? GetNameScope(visual);

                if (result != null)
                {
                    return result;
                }

                visual = (visual as ILogical)?.LogicalParent as Visual;
            }

            return null;
        }

        /// <summary>
        /// Gets the value of the attached <see cref="NameScopeProperty"/> on a visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <returns>The value of the NameScope attached property.</returns>
        public static INameScope GetNameScope(Visual visual)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            return visual.GetValue(NameScopeProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="NameScopeProperty"/> on a visual.
        /// </summary>
        /// <param name="visual">The visual.</param>
        /// <param name="value">The value to set.</param>
        public static void SetNameScope(Visual visual, INameScope value)
        {
            Contract.Requires<ArgumentNullException>(visual != null);

            visual.SetValue(NameScopeProperty, value);
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

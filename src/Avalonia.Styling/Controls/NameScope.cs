// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        /// <inheritdoc/>
        public bool IsCompleted { get; private set; }
        
        private readonly Dictionary<string, object> _inner = new Dictionary<string, object>();

        private readonly Dictionary<string, TaskCompletionSource<object>> _pendingSearches =
            new Dictionary<string, TaskCompletionSource<object>>();
        
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

        /// <inheritdoc />
        public void Register(string name, object element)
        {
            if (IsCompleted)
                throw new InvalidOperationException("NameScope is completed, no further registrations are allowed");
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
                if (_pendingSearches.TryGetValue(name, out var tcs))
                {
                    _pendingSearches.Remove(name);
                    tcs.SetResult(element);
                }
            }
        }

        public ValueTask<object> FindAsync(string name)
        {
            var found = Find(name);
            if (found != null)
                return new ValueTask<object>(found);
            if (IsCompleted)
                return new ValueTask<object>((object)null);
            if (!_pendingSearches.TryGetValue(name, out var tcs))
                // We are intentionally running continuations synchronously here
                _pendingSearches[name] = tcs = new TaskCompletionSource<object>();
            
            return new ValueTask<object>(tcs.Task);
        }

        /// <inheritdoc />
        public object Find(string name)
        {
            Contract.Requires<ArgumentNullException>(name != null);

            object result;
            _inner.TryGetValue(name, out result);
            return result;
        }

        public void Complete()
        {
            IsCompleted = true;
            foreach (var kp in _pendingSearches)
                kp.Value.TrySetResult(null);
            _pendingSearches.Clear();
        }

        
    }
}

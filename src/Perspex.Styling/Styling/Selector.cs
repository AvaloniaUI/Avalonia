// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Perspex.Styling
{
    /// <summary>
    /// A selector in a <see cref="Style"/>.
    /// </summary>
    /// <remarks>
    /// Selectors represented in markup using a CSS-like syntax, e.g. "Button &gt; .dark" which 
    /// means "A child of a Button with the 'dark' class applied. The preceeding example would be
    /// stored in 3 <see cref="Selector"/> objects, linked by the <see cref="Previous"/> property:
    /// <list type="number">
    /// <item>
    ///   <term>.dark</term>
    ///   <description>
    ///     A selector that selects a control with the 'dark' class applied.
    ///   </description>
    /// </item>
    /// <item>
    ///   <term>&gt;</term>
    ///   <description>
    ///     A selector that selects a child of the previous selector.
    ///   </description>
    /// </item>
    /// <item>
    ///   <term>Button</term>
    ///   <description>
    ///     A selector that selects a Button type.
    ///   </description>
    /// </item>
    /// </list>
    /// </remarks>
    public class Selector
    {
        private readonly Func<IStyleable, SelectorMatch> _evaluate;
        private readonly Type _targetType;
        private readonly bool _inTemplate;
        private readonly bool _stopTraversal;
        private string _description;

        /// <summary>
        /// Initializes a new instance of the <see cref="Selector"/> class.
        /// </summary>
        public Selector()
        {
            _evaluate = _ => SelectorMatch.True;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Selector"/> class.
        /// </summary>
        /// <param name="previous">The previous selector.</param>
        /// <param name="evaluate">The evaluator function.</param>
        /// <param name="selectorString">The string representation of the selector.</param>
        /// <param name="targetType">The target type, if available.</param>
        /// <param name="inTemplate">Whether to match in a control template.</param>
        /// <param name="stopTraversal">Whether to stop traversal at this point.</param>
        public Selector(
            Selector previous,
            Func<IStyleable, SelectorMatch> evaluate,
            string selectorString,
            Type targetType = null,
            bool inTemplate = false,
            bool stopTraversal = false)
            : this()
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            Previous = previous;
            _evaluate = evaluate;
            SelectorString = selectorString;
            _targetType = targetType;
            _inTemplate = inTemplate || previous._inTemplate;
            _stopTraversal = stopTraversal;
        }

        /// <summary>
        /// Gets the previous selector.
        /// </summary>
        public Selector Previous
        {
            get;
        }

        /// <summary>
        /// Gets a string representation of the selector.
        /// </summary>
        public string SelectorString
        {
            get;
        }

        /// <summary>
        /// Gets the target type of the selector, if available.
        /// </summary>
        public Type TargetType
        {
            get { return _targetType ?? MovePrevious()?.TargetType; }
        }

        /// <summary>
        /// Returns the previous selector if traversal is not stopped.
        /// </summary>
        /// <returns>The previous selector.</returns>
        public Selector MovePrevious()
        {
            return _stopTraversal ? null : Previous;
        }

        /// <summary>
        /// Tries to match the selector with a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>A <see cref="SelectorMatch"/>.</returns>
        public SelectorMatch Match(IStyleable control)
        {
            List<IObservable<bool>> inputs = new List<IObservable<bool>>();
            Selector selector = this;

            while (selector != null)
            {
                if (selector._inTemplate && control.TemplatedParent == null)
                {
                    return SelectorMatch.False;
                }

                var match = selector._evaluate(control);

                if (match.ImmediateResult == false)
                {
                    return match;
                }
                else if (match.ObservableResult != null)
                {
                    inputs.Add(match.ObservableResult);
                }

                selector = selector.MovePrevious();
            }

            if (inputs.Count > 0)
            {
                return new SelectorMatch(new StyleActivator(inputs));
            }
            else
            {
                return SelectorMatch.True;
            }
        }

        /// <summary>
        /// Gets a string representation of the selector.
        /// </summary>
        /// <returns>The string representation of the selector.</returns>
        public override string ToString()
        {
            if (_description == null)
            {
                string result = string.Empty;

                if (Previous != null)
                {
                    result = Previous.ToString();
                }

                _description = result + SelectorString;
            }

            return _description;
        }
    }
}

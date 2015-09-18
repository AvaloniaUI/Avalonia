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
    /// Selectors represented in markup using a CSS-like syntax, e.g. "Button &lt; .dark" which 
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
    ///   <term>&lt;</term>
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

        private readonly bool _inTemplate;

        private readonly bool _stopTraversal;

        private string _description;

        public Selector()
        {
            _evaluate = _ => SelectorMatch.True;
        }

        public Selector(
            Selector previous,
            Func<IStyleable, SelectorMatch> evaluate,
            string selectorString,
            bool inTemplate = false,
            bool stopTraversal = false)
            : this()
        {
            Contract.Requires<ArgumentNullException>(previous != null);

            Previous = previous;
            _evaluate = evaluate;
            SelectorString = selectorString;
            _inTemplate = inTemplate || previous._inTemplate;
            _stopTraversal = stopTraversal;
        }

        public Selector Previous
        {
            get; }

        public string SelectorString
        {
            get;
            set;
        }

        public Selector MovePrevious()
        {
            return _stopTraversal ? null : Previous;
        }

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

// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Avalonia.Styling
{
    /// <summary>
    /// A selector in a <see cref="Style"/>.
    /// </summary>
    public abstract class Selector
    {
        /// <summary>
        /// Gets a value indicating whether either this selector or a previous selector has moved
        /// into a template.
        /// </summary>
        public abstract bool InTemplate { get; }

        /// <summary>
        /// Gets a value indicating whether this selector is a combinator.
        /// </summary>
        /// <remarks>
        /// A combinator is a selector such as Child or Descendent which links simple selectors.
        /// </remarks>
        public abstract bool IsCombinator { get; }

        /// <summary>
        /// Gets the target type of the selector, if available.
        /// </summary>
        public abstract Type TargetType { get; }

        /// <summary>
        /// Tries to match the selector with a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="subscribe">
        /// Whether the match should subscribe to changes in order to track the match over time,
        /// or simply return an immediate result.
        /// </param>
        /// <returns>A <see cref="SelectorMatch"/>.</returns>
        public SelectorMatch Match(IStyleable control, bool subscribe = true)
        {
            List<IObservable<bool>> inputs = null;
            IObservable<bool> singleInput = null;

            var selector = this;
            var alwaysThisType = true;
            var hitCombinator = false;

            while (selector != null)
            {
                hitCombinator |= selector.IsCombinator;

                var match = selector.Evaluate(control, subscribe);

                if (!match.IsMatch)
                {
                    return hitCombinator ? SelectorMatch.NeverThisInstance : match;
                }
                else if (selector.InTemplate && control.TemplatedParent == null)
                {
                    return SelectorMatch.NeverThisInstance;
                }
                else if (match.Result == SelectorMatchResult.AlwaysThisInstance)
                {
                    alwaysThisType = false;
                }
                else if (match.Result == SelectorMatchResult.Sometimes)
                {
                    Debug.Assert(match.Activator != null);

                    if (inputs != null)
                    {
                        inputs.Add(match.Activator);
                    }
                    else
                    {
                        if (singleInput == null)
                        {
                            singleInput = match.Activator;
                        }
                        else
                        {
                            inputs = new List<IObservable<bool>>();

                            inputs.Add(singleInput);
                            inputs.Add(match.Activator);
                        }
                    }
                }

                selector = selector.MovePrevious();
            }

            if (inputs != null)
            {
                return new SelectorMatch(StyleActivator.And(inputs));
            }
            else if (singleInput != null)
            {
                return new SelectorMatch(singleInput);
            }
            else
            {
                return alwaysThisType && !hitCombinator ? 
                    SelectorMatch.AlwaysThisType :
                    SelectorMatch.AlwaysThisInstance;
            }
        }

        /// <summary>
        /// Evaluates the selector for a match.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="subscribe">
        /// Whether the match should subscribe to changes in order to track the match over time,
        /// or simply return an immediate result.
        /// </param>
        /// <returns>A <see cref="SelectorMatch"/>.</returns>
        protected abstract SelectorMatch Evaluate(IStyleable control, bool subscribe);

        /// <summary>
        /// Moves to the previous selector.
        /// </summary>
        protected abstract Selector MovePrevious();
    }
}

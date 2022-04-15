using System;
using Avalonia.Styling.Activators;

#nullable enable

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
        public abstract Type? TargetType { get; }

        /// <summary>
        /// Tries to match the selector with a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="parent">
        /// The parent style, if the style containing the selector is a nested style.
        /// </param>
        /// <param name="subscribe">
        /// Whether the match should subscribe to changes in order to track the match over time,
        /// or simply return an immediate result.
        /// </param>
        /// <returns>A <see cref="SelectorMatch"/>.</returns>
        public SelectorMatch Match(IStyleable control, IStyle? parent = null, bool subscribe = true)
        {
            // First match the selector until a combinator is found. Selectors are stored from 
            // right-to-left, so MatchUntilCombinator reverses this order because the type selector
            // will be on the left.
            var match = MatchUntilCombinator(control, this, parent, subscribe, out var combinator);
            
            // If the pre-combinator selector matches, we can now match the combinator, if any.
            if (match.IsMatch && combinator is object)
            {
                match = match.And(combinator.Match(control, parent, subscribe));

                // If we have a combinator then we can never say that we always match a control of
                // this type, because by definition the combinator matches on things outside of the
                // control.
                match = match.Result switch
                {
                    SelectorMatchResult.AlwaysThisType => SelectorMatch.AlwaysThisInstance,
                    SelectorMatchResult.NeverThisType => SelectorMatch.NeverThisInstance,
                    _ => match
                };
            }

            return match;
        }

        /// <summary>
        /// Evaluates the selector for a match.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="parent">
        /// The parent style, if the style containing the selector is a nested style.
        /// </param>
        /// <param name="subscribe">
        /// Whether the match should subscribe to changes in order to track the match over time,
        /// or simply return an immediate result.
        /// </param>
        /// <returns>A <see cref="SelectorMatch"/>.</returns>
        protected abstract SelectorMatch Evaluate(IStyleable control, IStyle? parent, bool subscribe);

        /// <summary>
        /// Moves to the previous selector.
        /// </summary>
        protected abstract Selector? MovePrevious();

        private static SelectorMatch MatchUntilCombinator(
            IStyleable control,
            Selector start,
            IStyle? parent,
            bool subscribe,
            out Selector? combinator)
        {
            combinator = null;

            var activators = new AndActivatorBuilder();
            var foundNested = false;
            var result = Match(control, start, parent, subscribe, ref activators, ref combinator, ref foundNested);

            if (parent is not null && !foundNested)
                throw new InvalidOperationException("Nesting selector '&' must appear in child selector.");

            return result == SelectorMatchResult.Sometimes ?
                new SelectorMatch(activators.Get()) :
                new SelectorMatch(result);
        }

        private static SelectorMatchResult Match(
            IStyleable control,
            Selector selector,
            IStyle? parent,
            bool subscribe,
            ref AndActivatorBuilder activators,
            ref Selector? combinator,
            ref bool foundNested)
        {
            var previous = selector.MovePrevious();

            // Selectors are stored from right-to-left, so we recurse into the selector in order to
            // reverse this order, because the type selector will be on the left and is our best
            // opportunity to exit early.
            if (previous != null && !previous.IsCombinator)
            {
                var previousMatch = Match(control, previous, parent, subscribe, ref activators, ref combinator, ref foundNested);

                if (previousMatch < SelectorMatchResult.Sometimes)
                {
                    return previousMatch;
                }
            }

            foundNested |= selector is NestingSelector;

            // Match this selector.
            var match = selector.Evaluate(control, parent, subscribe);

            if (!match.IsMatch)
            {
                combinator = null;
                return match.Result;
            }
            else if (match.Activator is object)
            {
                activators.Add(match.Activator!);
            }

            if (previous?.IsCombinator == true)
            {
                combinator = previous;
            }

            return match.Result;
        }
    }
}

// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Styling
{
    /// <summary>
    /// Describes how a <see cref="SelectorMatch"/> matches a control and its type.
    /// </summary>
    public enum SelectorMatchResult
    {
        /// <summary>
        /// The selector never matches this type.
        /// </summary>
        NeverThisType,

        /// <summary>
        /// The selector never matches this instance, but can match this type.
        /// </summary>
        NeverThisInstance,

        /// <summary>
        /// The selector always matches this type.
        /// </summary>
        AlwaysThisType,

        /// <summary>
        /// The selector always matches this instance, but doesn't always match this type.
        /// </summary>
        AlwaysThisInstance,

        /// <summary>
        /// The selector matches this instance based on the <see cref="SelectorMatch.Activator"/>.
        /// </summary>
        Sometimes,
    }

    /// <summary>
    /// Holds the result of a <see cref="Selector"/> match.
    /// </summary>
    /// <remarks>
    /// A selector match describes whether and how a <see cref="Selector"/> matches a control, and
    /// in addition whether the selector can ever match a control of the same type.
    /// </remarks>
    public class SelectorMatch
    {
        /// <summary>
        /// A selector match with the result of <see cref="SelectorMatchResult.NeverThisType"/>.
        /// </summary>
        public static readonly SelectorMatch NeverThisType = new SelectorMatch(SelectorMatchResult.NeverThisType);

        /// <summary>
        /// A selector match with the result of <see cref="SelectorMatchResult.NeverThisInstance"/>.
        /// </summary>
        public static readonly SelectorMatch NeverThisInstance = new SelectorMatch(SelectorMatchResult.NeverThisInstance);

        /// <summary>
        /// A selector match with the result of <see cref="SelectorMatchResult.AlwaysThisType"/>.
        /// </summary>
        public static readonly SelectorMatch AlwaysThisType = new SelectorMatch(SelectorMatchResult.AlwaysThisType);

        /// <summary>
        /// Gets a selector match with the result of <see cref="SelectorMatchResult.AlwaysThisInstance"/>.
        /// </summary>
        public static readonly SelectorMatch AlwaysThisInstance = new SelectorMatch(SelectorMatchResult.AlwaysThisInstance);

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectorMatch"/> class with a 
        /// <see cref="SelectorMatchResult.Sometimes"/> result.
        /// </summary>
        /// <param name="match">The match activator.</param>
        public SelectorMatch(IObservable<bool> match)
        {
            Contract.Requires<ArgumentNullException>(match != null);

            Result = SelectorMatchResult.Sometimes;
            Activator = match;
        }

        private SelectorMatch(SelectorMatchResult result) => Result = result;

        /// <summary>
        /// Gets a value indicating whether the match was positive.
        /// </summary>
        public bool IsMatch => Result >= SelectorMatchResult.AlwaysThisType;

        /// <summary>
        /// Gets the result of the match.
        /// </summary>
        public SelectorMatchResult Result { get; }

        /// <summary>
        /// Gets an observable which tracks the selector match, in the case of selectors that can
        /// change over time.
        /// </summary>
        public IObservable<bool> Activator { get; }
    }
}

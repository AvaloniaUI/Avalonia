﻿// -----------------------------------------------------------------------
// <copyright file="SelectorMatch.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;

    /// <summary>
    /// Holds the result of a <see cref="Selector"/> match.
    /// </summary>
    /// <remarks>
    /// There are two types of selectors - ones whose match can never change for a particular
    /// control (such as <see cref="Selectors.OfType{T}(Selector)"/>) and ones whose result can
    /// change over time (such as <see cref="Selectors.Class(Selector, string)"/>. For the first
    /// category of selectors, the value of <see cref="ImmediateResult"/> will be set but for the
    /// second, <see cref="ImmediateResult"/> will be null and <see cref="ObservableResult"/> will
    /// hold an observable which tracks the match.
    /// </remarks>
    public class SelectorMatch
    {
        public static readonly SelectorMatch False = new SelectorMatch(false);

        public static readonly SelectorMatch True = new SelectorMatch(true);

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectorMatch"/> class.
        /// </summary>
        /// <param name="match">The immediate match value.</param>
        public SelectorMatch(bool match)
        {
            this.ImmediateResult = match;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectorMatch"/> class.
        /// </summary>
        /// <param name="match">The observable match value.</param>
        public SelectorMatch(IObservable<bool> match)
        {
            this.ObservableResult = match;
        }

        /// <summary>
        /// Gets the immedate result of the selector match, in the case of selectors that cannot
        /// change over time.
        /// </summary>
        public bool? ImmediateResult { get; }

        /// <summary>
        /// Gets an observable which tracks the selector match, in the case of selectors that can
        /// change over time.
        /// </summary>
        public IObservable<bool> ObservableResult { get; }
    }
}

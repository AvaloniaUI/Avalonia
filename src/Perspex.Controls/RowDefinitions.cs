// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using Perspex.Collections;

namespace Perspex.Controls
{
    /// <summary>
    /// A collection of <see cref="RowDefinition"/>s.
    /// </summary>
    public class RowDefinitions : PerspexList<RowDefinition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RowDefinitions"/> class.
        /// </summary>
        public RowDefinitions()
        {
            ResetBehavior = ResetBehavior.Remove;
            CollectionChanged += (s, e) => Changed?.Invoke(this, EventArgs.Empty);
            this.TrackItemPropertyChanged(_ => Changed?.Invoke(this, EventArgs.Empty));
        }

        /// <summary>
        /// Called when items or item properties are changed.
        /// </summary>
        internal event EventHandler Changed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RowDefinitions"/> class.
        /// </summary>
        /// <param name="s">A string representation of the row definitions.</param>
        public RowDefinitions(string s)
            : this()
        {
            AddRange(GridLength.ParseLengths(s, CultureInfo.InvariantCulture).Select(x => new RowDefinition(x)));
        }
    }
}
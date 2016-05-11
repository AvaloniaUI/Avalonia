// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;

namespace Avalonia.Markup
{
    /// <summary>
    /// Provides a set of useful <see cref="IValueConverter"/>s for working with string values.
    /// </summary>
    public static class BoolConverters
    {
        /// <summary>
        /// A multi-value converter that returns true if all inputs are true.
        /// </summary>
        public static readonly IMultiValueConverter And =
            new FuncMultiValueConverter<bool, bool>(x => x.All(y => y));
    }
}

// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using Avalonia.Utilities;

namespace Avalonia.Markup
{
    /// <summary>
    /// Provides a set of useful <see cref="IValueConverter"/>s for working with string values.
    /// </summary>
    public static class StringConverters
    {
        /// <summary>
        /// A value converter that returns true if the input string is null or an empty string.
        /// </summary>
        public static readonly IValueConverter NullOrEmpty =
            new FuncValueConverter<string, bool>(string.IsNullOrEmpty);

        /// <summary>
        /// A value converter that returns true if the input string is not null or empty.
        /// </summary>
        public static readonly IValueConverter NotNullOrEmpty =
            new FuncValueConverter<string, bool>(x => !string.IsNullOrEmpty(x));
    }
}

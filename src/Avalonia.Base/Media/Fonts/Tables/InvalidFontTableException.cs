// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

using System;

namespace Avalonia.Media.Fonts.Tables
{
    /// <summary>
    /// Exception font loading can throw if it encounters invalid data during font loading.
    /// </summary>
    /// <seealso cref="Exception" />
    internal class InvalidFontTableException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidFontTableException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="table">The table.</param>
        public InvalidFontTableException(string message, string table)
            : base(message)
            => Table = table;

        /// <summary>
        /// Gets the table where the error originated.
        /// </summary>
        public string Table { get; }
    }
}

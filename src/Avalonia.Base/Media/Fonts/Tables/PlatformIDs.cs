// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/blob/034a440aece357341fcc6b02db58ffbe153e54ef/src/SixLabors.Fonts

namespace Avalonia.Media.Fonts.Tables
{
    /// <summary>
    /// platforms ids
    /// </summary>
    internal enum PlatformIDs : ushort
    {
        /// <summary>
        /// Unicode platform
        /// </summary>
        Unicode = 0,

        /// <summary>
        /// Script manager code
        /// </summary>
        Macintosh = 1,

        /// <summary>
        /// [deprecated] ISO encoding
        /// </summary>
        ISO = 2,

        /// <summary>
        /// Window encoding
        /// </summary>
        Windows = 3,

        /// <summary>
        /// Custom platform
        /// </summary>
        Custom = 4 // Custom  None
    }
}

// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Visuals.Media.Imaging
{
    /// <summary>
    /// Controls the performance and quality of bitmap scaling.
    /// </summary>
    public enum BitmapInterpolationMode
    {
        /// <summary>
        /// Uses the default behavior of the underling render backend.
        /// </summary>
        Default,

        /// <summary>
        /// The best performance but worst image quality.
        /// </summary>
        LowQuality,

        /// <summary>
        /// Good performance and decent image quality.
        /// </summary>
        MediumQuality,      

        /// <summary>
        /// Highest quality but worst performance.
        /// </summary>
        HighQuality
    }
}

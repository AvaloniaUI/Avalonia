namespace Avalonia.Media.Imaging
{
    /// <summary>
    /// Controls the performance and quality of bitmap scaling.
    /// </summary>
    public enum BitmapInterpolationMode : byte
    {
        Unspecified,

        /// <summary>
        /// Disable interpolation.
        /// </summary>
        None,

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

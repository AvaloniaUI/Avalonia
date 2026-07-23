using System;
using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    /// <summary>
    /// Reports the color space that the content of a top level is presented in.
    /// </summary>
    [Unstable]
    public interface IColorManagedPresentation
    {
        /// <summary>
        /// Gets the color space content is presented in, or
        /// <see cref="PresentationColorSpace.Unspecified"/> when it is presented without one.
        /// </summary>
        /// <remarks>
        /// This is always a concrete color space, a request like
        /// <see cref="PresentationColorSpace.WideGamut"/> is never reported back. It is known before
        /// the first frame is drawn, but it can still change afterwards, so an application which
        /// converts its content should follow <see cref="CurrentColorSpaceChanged"/> instead of
        /// reading it only once.
        /// </remarks>
        PresentationColorSpace CurrentColorSpace { get; }

        /// <summary>
        /// Raised on the UI thread when <see cref="CurrentColorSpace"/> changed.
        /// </summary>
        /// <remarks>
        /// This happens when the requested color space turns out to be unavailable, and when the
        /// presentation changes later, for example after the window was moved to a display with a
        /// different color space.
        /// </remarks>
        event EventHandler? CurrentColorSpaceChanged;
    }
}

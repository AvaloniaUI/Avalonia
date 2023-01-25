using System.Threading.Tasks;
using Avalonia.Controls;

namespace Avalonia.Diagnostics.Screenshots
{
    /// <summary>
    /// Base class for render Screenshto to stream
    /// </summary>
    public abstract class BaseRenderToStreamHandler : IScreenshotHandler
    {
        /// <summary>
        /// Get stream to write a screenshot to.
        /// </summary>
        /// <param name="control"></param>
        /// <returns>stream to render the control</returns>
        protected abstract Task<System.IO.Stream?> GetStream(Control control);

        public async Task Take(Control control)
        {
#if NET6_0_OR_GREATER
            await using var output = await GetStream(control);
#else
            using var output = await GetStream(control);
#endif
            if (output is { })
            {
                control.RenderTo(output);
                await output.FlushAsync();
            }
        }
    }
}

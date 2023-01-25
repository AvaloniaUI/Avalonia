using System.Threading.Tasks;
using Avalonia.Controls;

namespace Avalonia.Diagnostics
{
    /// <summary>
    /// Allowed to define custom handler for Shreeshot
    /// </summary>
    public interface IScreenshotHandler
    {
        /// <summary>
        /// Handle the Screenshot
        /// </summary>
        /// <returns></returns>
        Task Take(Control control);
    }
}

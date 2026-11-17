using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Controls.Platform
{
    [NotClientImplementable]
    public interface IViewLauncher
    {
        bool SupportsMultiWindow { get; }
        Task<IViewHandle?>? ShowView(object content, Point initialPosition, Size initialSize);
    }

    public interface IViewHandle
    {
        TopLevel? TopLevel { get; }

        Task? DialogTask { get; }

        public void Close(object? result = null);
    }
}

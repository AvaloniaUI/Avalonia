using System.Threading.Tasks;

namespace Avalonia.Input.Platform
{
    public interface IClipboard
    {
        Task<string> GetTextAsync();

        Task SetTextAsync(string text);

        Task ClearAsync();
    }
}

using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.Platform
{
    [NotClientImplementable]
    public interface IClipboard
    {
        Task<string?> GetTextAsync();

        Task SetTextAsync(string? text);

        Task ClearAsync();

        Task SetDataObjectAsync(IDataObject data);
        
        Task<string[]> GetFormatsAsync();
        
        Task<object?> GetDataAsync(string format);
    }
}

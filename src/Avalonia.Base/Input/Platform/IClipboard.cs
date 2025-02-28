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
        
        /// <summary>
        /// If clipboard contains the IDataObject that was set by a previous call to <see cref="SetDataObjectAsync"/>,
        /// return said IDataObject instance. Otherwise, return null.
        /// Note that not every platform supports that method, on unsupported platforms this method will always return
        /// null
        /// </summary>
        /// <returns></returns>
        Task<IDataObject?> TryGetInProcessDataObjectAsync();
    }
}

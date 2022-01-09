using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace Avalonia.Input.Platform
{
    public interface IClipboard
    {
        Task<string> GetTextAsync();

        Task SetTextAsync(string text);
        
        Task<Bitmap> GetBitmapAsync();
        
        Task SetBitmapAsync(Bitmap bitmap);

        Task ClearAsync();

        Task SetDataObjectAsync(IDataObject data);
        
        Task<string[]> GetFormatsAsync();
        
        Task<object> GetDataAsync(string format);
       
    }
}

using System.Threading.Tasks;
using Android.Content;
using Avalonia.Android.Platform.Specific.Helpers;
using Avalonia.Input;

namespace Avalonia.Android.Platform
{
    internal class DataTransferToClipDataWrapper(IDataTransfer dataTransfer)
    {
        public Task<ClipData?> GetClipData() => ClipDataHelper.GetClipData(dataTransfer, this);
    }
}

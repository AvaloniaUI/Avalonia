using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.Platform;

[PrivateApi]
public interface IOwnedClipboardImpl : IClipboardImpl
{
    Task<bool> IsCurrentOwnerAsync();
}

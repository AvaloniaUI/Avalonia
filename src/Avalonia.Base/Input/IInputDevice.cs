using Avalonia.Input.Raw;
using Avalonia.Metadata;

namespace Avalonia.Input
{
    [NotClientImplementable, PrivateApi]
    public interface IInputDevice
    {
        /// <summary>
        /// Processes raw event. Is called after preprocessing by InputManager
        /// </summary>
        /// <param name="ev"></param>
        void ProcessRawEvent(RawInputEventArgs ev);
    }
}

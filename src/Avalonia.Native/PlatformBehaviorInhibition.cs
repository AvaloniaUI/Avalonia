using System.Threading.Tasks;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    internal class PlatformBehaviorInhibition : IPlatformBehaviorInhibition
    {
        readonly IAvnPlatformBehaviorInhibition _native;

        internal PlatformBehaviorInhibition(IAvnPlatformBehaviorInhibition native)
            => _native = native;

        public Task SetInhibitAppSleep(bool inhibitAppSleep, string reason)
        {
            _native.SetInhibitAppSleep(inhibitAppSleep ? 1 : 0, reason);
            return Task.CompletedTask;
        }
    }
}

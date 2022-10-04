using System;
using System.Threading.Tasks;

namespace Avalonia.Platform
{
    internal class PlatformBehaviorInhibitionDisposer : IDisposable
    {
        readonly Action _disposeAction;

        public PlatformBehaviorInhibitionDisposer(Action disposeAction)
            => _disposeAction = disposeAction;

        public void Dispose()
            => _disposeAction();
    }

    public enum PlatformBehaviorInhibitionType
    {
        AppSleep
    }

    public interface IPlatformBehaviorInhibition
    {
        Task SetInhibitAppSleep(bool inhibitAppSleep, string reason);
    }

    public abstract class PlatformBehavior
    {
        public static async Task<IDisposable> RequestPlatformBehaviorInhibition(PlatformBehaviorInhibitionType type, string reason)
        {
            var platformBehaviorInhibition = AvaloniaLocator.Current.GetService<IPlatformBehaviorInhibition>();
            if (platformBehaviorInhibition is null)
            {
                return new PlatformBehaviorInhibitionDisposer(() => { });
            }

            switch (type)
            {
                case PlatformBehaviorInhibitionType.AppSleep:
                    await platformBehaviorInhibition.SetInhibitAppSleep(true, reason);
                    return new PlatformBehaviorInhibitionDisposer(
                        () => platformBehaviorInhibition.SetInhibitAppSleep(false, string.Empty));
                default:
                    return new PlatformBehaviorInhibitionDisposer(() => { });
            }
        }
    }
}
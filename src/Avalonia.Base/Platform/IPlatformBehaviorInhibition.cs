using System.Threading.Tasks;

namespace Avalonia.Platform
{
    /// <summary>
    /// Allows to inhibit platform specific behavior.
    /// </summary>
    public interface IPlatformBehaviorInhibition
    {
        Task SetInhibitAppSleep(bool inhibitAppSleep, string reason);
    }
}

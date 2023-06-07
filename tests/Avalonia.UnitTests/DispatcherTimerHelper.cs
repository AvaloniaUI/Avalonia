using Avalonia.Threading;

namespace Avalonia.UnitTests;

public static class DispatcherTimerUtils
{
    public static void ForceFire(this DispatcherTimer timer)
    {
        timer.Promote();
        timer.Dispatcher.RemoveTimer(timer);
        timer.Dispatcher.RunJobs();
    }
}
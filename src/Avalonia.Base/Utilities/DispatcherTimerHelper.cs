using Avalonia.Threading;

namespace Avalonia.Utilities;

public class DispatcherTimerHelper
{
    
}

public static class DispatcherTimerUtils
{
    public static void ForceFire(this DispatcherTimer timer)
    {
        timer.Promote();
        timer.Dispatcher.RunJobs();
    }
}
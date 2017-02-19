using Android.App;
using Android.OS;

namespace Avalonia.Android
{
    internal class ActivityTracker : Java.Lang.Object, global::Android.App.Application.IActivityLifecycleCallbacks
    {
        public static Activity Current { get; private set; }
        public void OnActivityCreated(Activity activity, Bundle savedInstanceState)
        {
            Current = activity;
        }

        public void OnActivityDestroyed(Activity activity)
        {
            if (Current == activity)
                Current = null;
        }

        public void OnActivityPaused(Activity activity)
        {
            if (Current == activity)
                Current = null;
        }

        public void OnActivityResumed(Activity activity)
        {
            Current = activity;
        }

        public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
        {
            Current = activity;
        }

        public void OnActivityStarted(Activity activity)
        {
            Current = activity;
        }

        public void OnActivityStopped(Activity activity)
        {
            if (Current == activity)
                Current = null;
        }
    }
}
using System;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;

namespace Avalonia.Android.Platform;

internal static class PlatformSupport
{
    private static int s_lastRequestCode = 20000;

    public static int GetNextRequestCode() => s_lastRequestCode++;

    public static async Task<bool> CheckPermission(this Activity activity, string permission)
    {
        if (activity is not IActivityResultHandler mainActivity)
        {
            throw new InvalidOperationException("Main activity must implement IActivityResultHandler interface.");
        }

        if (!OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            return true;
        }

        if (activity.CheckSelfPermission(permission) == Permission.Granted)
        {
            return true;
        }
        
        var currentRequestCode = GetNextRequestCode();
        var tcs = new TaskCompletionSource<bool>();
        mainActivity.RequestPermissionsResult += RequestPermissionsResult;
        activity.RequestPermissions(new [] { permission }, currentRequestCode);

        return await tcs.Task;
        
        void RequestPermissionsResult(int requestCode, string[] arg2, Permission[] arg3)
        {
            if (currentRequestCode != requestCode)
            {
                return;
            }

            mainActivity.RequestPermissionsResult -= RequestPermissionsResult;

            _ = tcs.TrySetResult(arg3.All(p => p == Permission.Granted));
        }
    }
}

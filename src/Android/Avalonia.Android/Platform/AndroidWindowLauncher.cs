using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace Avalonia.Android.Platform
{
    /*internal class ViewHandle : IViewHandle
    {
        public ViewHandle(TopLevel? topLevel)
        {
            TopLevel = topLevel;
            _tcs = new TaskCompletionSource<object?>();
            if (Activity is AvaloniaActivity activity)
            {
                activity.Closed += OnActivityClose;
            }
        }

        public TopLevel? TopLevel { get; private set; }

        private TaskCompletionSource<object?> _tcs;

        public Task? DialogTask => _tcs.Task;

        internal TopLevelImpl? TopLevelImpl => TopLevel?.PlatformImpl as TopLevelImpl;

        internal Activity? Activity => TopLevelImpl?.Activity;

        public void Close(object? result)
        {
            if (TopLevelImpl?.Activity is AvaloniaActivity activity)
            {
                activity.Closed -= OnActivityClose;
                _tcs.SetResult(result);
                TopLevel = null;
                activity.Finish();
            }
        }

        private void OnActivityClose(object? sender, EventArgs eventArgs)
        {
            TopLevel = null;
            _tcs.SetResult(null);
        }
    }*/
    /*
    internal class AndroidWindowLauncher(TopLevelImpl topLevelImpl) : IViewLauncher
    {
        public const string RequestIdName = "avalonia_request_id";
        private static ConcurrentDictionary<int, WindowRequest?> s_pendingRequests = new ConcurrentDictionary<int, WindowRequest?>();
        private static int _requestId = 0;

        private int NextId => Interlocked.Increment(ref _requestId);

        public Task<IViewHandle?>? ShowView(object content, Point initialPosition, Size initialSize)
        {
            if (topLevelImpl.Activity is not AvaloniaActivity activity)
                return null;
            var tcs = new TaskCompletionSource<IViewHandle?>();

            var id = NextId;
            var intent = CreateIntent(GetWindowAcitivityType(), id);

            var request = new WindowRequest<IViewHandle?>(content, tcs);
            s_pendingRequests.TryAdd(id, request);

            var desktopScaling = topLevelImpl.DesktopScaling;
            var rect = new Rect(initialPosition * desktopScaling, initialSize * desktopScaling);

            var options = ActivityOptions.MakeBasic();

            if (topLevelImpl.Activity.IsInMultiWindowMode == true)
            {
                var r = new global::Android.Graphics.Rect((int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom);
                options = options?.SetLaunchBounds(r);
            }

            activity.StartActivity(intent, options?.ToBundle());

            return tcs.Task;
        }

        public bool SupportsMultiWindow => IAndroidApplication.WindowActivityType != null;

        public static WindowRequest? GetWindowRequest(int key)
        {
            if (s_pendingRequests.TryGetValue(key, out var request))
                return request;

            return null;
        }

        private static Intent CreateIntent(Type type, int id)
        {
            var intent = new Intent(global::Android.App.Application.Context, type);
            intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.MultipleTask);
            intent.AddFlags(ActivityFlags.LaunchAdjacent);
            intent.PutExtra(RequestIdName, id);
            return intent;
        }

        private Type GetWindowAcitivityType()
        {
            return IAndroidApplication.WindowActivityType ?? throw new InvalidOperationException();
        }
    }*/
}

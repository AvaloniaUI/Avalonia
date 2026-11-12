using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Avalonia.Android.Platform;
using Avalonia.Controls;
using Avalonia.Platform;

namespace Avalonia.Android.Scene
{
    internal class SceneManager(Context context) : ISceneManager
    {
        public const string RequestIdName = "avalonia_request_id";
        private static ConcurrentDictionary<int, WindowRequest?> s_pendingRequests = new ConcurrentDictionary<int, WindowRequest?>();
        private static int _requestId = 0;

        private int NextId => Interlocked.Increment(ref _requestId);

        private Dictionary<Type, Type> _activityTypes = new Dictionary<Type, Type>();

        public Task<bool> ShowScene(Controls.Scene scene, SceneLaunchOptions? options = null)
        {
            var tcs = new TaskCompletionSource<bool>();

            var id = NextId;
            var initialSize = new Point(scene.Width, scene.Height);

            var desktopScaling = context.Resources?.DisplayMetrics?.Density ?? 1;
            var rect = new Rect(scene.InitialPosition * desktopScaling, initialSize * desktopScaling);
            var intent = CreateIntent(GetSceneActivityType(scene), id);

            var request = new WindowRequest(scene, tcs);
            s_pendingRequests.TryAdd(id, request);

            var activityOptions = ActivityOptions.MakeBasic();

            var r = new global::Android.Graphics.Rect((int)rect.Left, (int)rect.Top, (int)rect.Right, (int)rect.Bottom);
            activityOptions = activityOptions?.SetLaunchBounds(r);

            if(options != null)
            {
                if(OperatingSystem.IsAndroidVersionAtLeast(26) && options.Display?.TryGetPlatformHandle() is PlatformHandle handle)
                {
                    activityOptions = activityOptions?.SetLaunchDisplayId((int)handle.Handle);
                }
            }

            context.StartActivity(intent, activityOptions?.ToBundle());

            return tcs.Task;
        }

        private Type GetSceneActivityType(Controls.Scene scene)
        {
            var sceneType = scene.GetType();
            if (_activityTypes.TryGetValue(sceneType, out var activityType))
            {
                return activityType;
            }
            throw new InvalidOperationException($"No registered activity type for scene type {sceneType.FullName}");
        }

        public void RegisterActivityType<T>(Type sceneActivityType)
        {
            _activityTypes[typeof(T)] = sceneActivityType;
        }

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
    }

    internal record class WindowRequest(Controls.Scene Scene, TaskCompletionSource<bool> TaskCompletionSource)
    {
        public void SetComplete()
        {
            if(!TaskCompletionSource.Task.IsCompleted)
            {
                TaskCompletionSource.SetResult(true);
            }
        }
    }
}

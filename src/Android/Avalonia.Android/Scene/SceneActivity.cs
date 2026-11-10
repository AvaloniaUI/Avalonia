using System;
using Android.OS;
using Avalonia.Controls;

namespace Avalonia.Android.Scene
{
    public abstract class SceneActivity : AvaloniaActivity, ISceneHost
    {
        public void Close()
        {
            Finish();
        }
    }

    public abstract class SceneActivity<T> : SceneActivity where T : Controls.Scene, new()
    {
        public T? Scene { get; set; }

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            var requestKey = Intent?.GetIntExtra(SceneManager.RequestIdName, -1) ?? -1;

            WindowRequest? request = null;

            if (requestKey != -1)
            {
                request = SceneManager.GetWindowRequest(requestKey);

                if (request != null)
                    Scene = (T?)request.Scene;
            }

            Scene ??= new T();

            base.OnCreate(savedInstanceState);

            Content = Scene;

            Scene.AttachHost(this);

            request?.SetComplete();
        }

        protected static void RegisterInManager<TScene>(Type activityType) where TScene : Controls.Scene, new()
        {
            var manager = AvaloniaLocator.Current.GetService<ISceneManager>() as SceneManager;
            manager?.RegisterActivityType<TScene>(activityType);
        }
    }
}

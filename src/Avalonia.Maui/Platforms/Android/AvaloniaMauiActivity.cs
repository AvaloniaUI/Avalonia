using Android.App;
using Android.Content;
using Avalonia.Android;

namespace Avalonia.Maui.Platforms.Android
{
    public class AvaloniaMauiActivity : MauiAppCompatActivity, IActivityResultHandler
    {
        public Action<int, Result, Intent> ActivityResult { get; set; }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            ActivityResult?.Invoke(requestCode, resultCode, data);
        }
    }
}

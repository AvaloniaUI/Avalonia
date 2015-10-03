using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Perspex.Android.Rendering;

namespace Perspex.Android
{
    public class PerspexActivity : Activity
    {
        internal static PerspexActivity Instance { get; private set; }
        internal Canvas Canvas { get; set; }
        internal PerspexView View { get; set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            Instance = this;
            RequestWindowFeature(WindowFeatures.NoTitle);
            base.OnCreate(savedInstanceState);
        }
    }
}
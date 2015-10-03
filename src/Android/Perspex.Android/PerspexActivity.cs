using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Perspex.Android.Rendering;
using Perspex.Input;
using Perspex.Input.Raw;
using Perspex.Platform;


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
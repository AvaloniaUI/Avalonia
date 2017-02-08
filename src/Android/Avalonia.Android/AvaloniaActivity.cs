using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Avalonia.Android
{
    public abstract class AvaloniaActivity : Activity
    {
        
        internal AvaloniaView View;
        object _content;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            RequestWindowFeature(WindowFeatures.NoTitle);
            View = new AvaloniaView(this);
            if(_content != null)
                View.Content = _content;
            SetContentView(View);
            TakeKeyEvents(true);
            base.OnCreate(savedInstanceState);
        }

        public object Content
        {
            get
            {
                return _content;
            }
            set
            {
                _content = value;
                if (View != null)
                    View.Content = value;
            }
        }

        public override bool DispatchKeyEvent(KeyEvent e)
        {
            return View.DispatchKeyEvent(e);
        }
    }
}
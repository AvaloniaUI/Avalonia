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
        AvaloniaView _view;
        object _content;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            RequestWindowFeature(WindowFeatures.NoTitle);
            _view = new AvaloniaView(this);
            if(_content != null)
                _view.Content = _content;
            SetContentView(_view);
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
                if (_view != null)
                    _view.Content = value;
            }
        }

        public override bool DispatchKeyEvent(KeyEvent e)
        {
            return _view.DispatchKeyEvent(e);
        }
    }
}
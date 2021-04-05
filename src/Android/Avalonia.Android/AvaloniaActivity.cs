using Android.App;
using Android.OS;
using Android.Views;

namespace Avalonia.Android
{
    public abstract class AvaloniaActivity : Activity
    {
        internal AvaloniaView View;
        object _content;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            View = new AvaloniaView(this);
            if (_content != null)
                View.Content = _content;
            SetContentView(View);
            //TakeKeyEvents(true);
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

        //public override bool DispatchKeyEvent(KeyEvent e) =>
        //    View.DispatchKeyEvent(e) ? true : base.DispatchKeyEvent(e);
    }
}

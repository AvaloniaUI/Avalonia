using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace Perspex.Android.Platform.Specific
{
    public class PerspexActivity : Activity, IAndroidActivity
    {
        private IAndroidView _contentView;

        public Activity Activity => this;

        public IAndroidView ContentView
        {
            get
            {
                return this._contentView;
            }

            set
            {
                this._contentView = value;
                var fl = new FrameLayout(this);
                fl.AddView(this._contentView.View);
                //this.SetContentView(value.View);
                this.SetContentView(fl);
            }
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            PerspexLocator.CurrentMutable.Bind<IAndroidActivity>().ToConstant(this);
            RequestWindowFeature(WindowFeatures.NoTitle);
            base.OnCreate(savedInstanceState);
        }

        public override void SetContentView(View view)
        {
            base.SetContentView(view);
            TakeKeyEvents(true);
        }

        public override bool DispatchKeyEvent(KeyEvent e)
        {
            if (_contentView != null)
            {
                _contentView.View.DispatchKeyEvent(e);
            }

            return base.DispatchKeyEvent(e);
        }
    }
}
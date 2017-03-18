using Avalonia.Media;
using CoreGraphics;
using UIKit;

namespace Avalonia.iOS
{
    class AvaloniaRootViewController : UIViewController
    {
        private object _content;
        private Color _statusBarColor = Colors.White;

        public object Content
        {
            get { return _content; }
            set
            {
                _content = value;
                var view = (View as AvaloniaView);
                if (view != null)
                    view.Content = value;
            }
        }

        public Color StatusBarColor
        {
            get { return _statusBarColor; }
            set
            {
                _statusBarColor = value;
                var view = (View as AvaloniaView);
                if (view != null)
                    view.BackgroundColor = value.ToUiColor();
            }
        }

        void AutoFit()
        {
            var needFlip = !UIDevice.CurrentDevice.CheckSystemVersion(8, 0) &&
               (InterfaceOrientation == UIInterfaceOrientation.LandscapeLeft
                || InterfaceOrientation == UIInterfaceOrientation.LandscapeRight);
            // Bounds here (if top level) needs to correspond with the rendertarget 
            var frame = UIScreen.MainScreen.Bounds;
            if (needFlip)
                frame = new CGRect(frame.Y, frame.X, frame.Height, frame.Width);
            ((AvaloniaView) View).Padding =
                new Thickness(0, UIApplication.SharedApplication.StatusBarFrame.Size.Height, 0, 0);
            View.Frame = frame;
        }

        public override void LoadView()
        {
            View = new AvaloniaView() {Content = Content, BackgroundColor = _statusBarColor.ToUiColor()};
            UIApplication.Notifications.ObserveDidChangeStatusBarOrientation(delegate { AutoFit(); });
            UIApplication.Notifications.ObserveDidChangeStatusBarFrame(delegate { AutoFit(); });
            AutoFit();
        }
    }
}
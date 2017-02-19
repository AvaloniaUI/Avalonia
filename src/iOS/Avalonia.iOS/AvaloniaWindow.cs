using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Media;
using Foundation;
using UIKit;

namespace Avalonia.iOS
{
    public sealed class AvaloniaWindow : UIWindow
    {
        readonly AvaloniaRootViewController _controller = new AvaloniaRootViewController();
        public object Content
        {
            get { return _controller.Content; }
            set { _controller.Content = value; }
        }

        public AvaloniaWindow() : base(UIScreen.MainScreen.Bounds)
        {
            RootViewController = _controller;
        }

        public Color StatusBarColor
        {
            get { return _controller.StatusBarColor; }
            set { _controller.StatusBarColor = value; }
        }
    }
}
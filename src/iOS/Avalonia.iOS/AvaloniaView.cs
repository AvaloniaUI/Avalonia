using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls.Embedding;
using CoreGraphics;
using UIKit;

namespace Avalonia.iOS
{
    public class AvaloniaView : UIView
    {
        private EmbeddableImpl _impl;
        private EmbeddableControlRoot _root;
        private Thickness _padding;

        public Thickness Padding
        {
            get { return _padding; }
            set
            {
                _padding = value;
                SetNeedsLayout();
            }
        }

        public AvaloniaView()
        {
            
            _impl = new EmbeddableImpl();
            AddSubview(_impl);
            BackgroundColor = UIColor.White;
            AutoresizingMask = UIViewAutoresizing.All;
            _root = new EmbeddableControlRoot(_impl);
            _root.Prepare();
        }

        public override void LayoutSubviews()
        {
            _impl.Frame = new CGRect(Padding.Left, Padding.Top,
                Frame.Width - Padding.Left - Padding.Right,
                Frame.Height - Padding.Top - Padding.Bottom);
        }


        public object Content
        {
            get { return _root.Content; }
            set { _root.Content = value; }
        }
    }
}

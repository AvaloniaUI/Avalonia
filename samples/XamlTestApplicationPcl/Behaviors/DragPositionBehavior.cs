using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex;
using Perspex.Controls;
using Perspex.Input;
using Perspex.Media;
using Perspex.Xaml.Interactivity;

namespace XamlTestApplication.Behaviors
{
    public class DragPositionBehavior : PerspexObject, IBehavior
    {
        public PerspexObject AssociatedObject { get; set; }

        public void Attach(PerspexObject associatedObject)
        {
            if ((associatedObject != AssociatedObject) && !Design.IsDesignMode)
            {
                AssociatedObject = associatedObject;
                var fe = AssociatedObject as Control;
                if (fe != null)
                {
                    fe.PointerPressed += AssociatedObject_PointerPressed;
                }
            }
        }

        private Control parent = null;
        private Point prevPoint;

        private void AssociatedObject_PointerPressed(object sender, PointerPressEventArgs e)
        {
            var fe = AssociatedObject as Control;
            parent = (Control)fe.Parent;

            if (!(fe.RenderTransform is TranslateTransform))
                fe.RenderTransform = new TranslateTransform();

            prevPoint = e.GetPosition(parent);
            parent.PointerMoved += Parent_PointerMoved;
            parent.PointerReleased += Parent_PointerReleased;
        }

        private void Parent_PointerMoved(object o, PointerEventArgs args)
        {
            var fe = AssociatedObject as Control;
            var pos = args.GetPosition(parent);
            var tr = (TranslateTransform)fe.RenderTransform;
            tr.X += pos.X - prevPoint.X;
            tr.Y += pos.Y - prevPoint.Y;
            prevPoint = pos;
        }

        private void Parent_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            parent.PointerMoved -= Parent_PointerMoved;
            parent.PointerReleased -= Parent_PointerReleased;
            parent = null;
        }

        public void Detach()
        {
            var fe = AssociatedObject as Control;
            if (fe != null)
            {
                fe.PointerPressed -= AssociatedObject_PointerPressed;
            }

            parent = null;
            AssociatedObject = null;
        }
    }
}

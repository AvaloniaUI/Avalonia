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
        public PerspexObject AssociatedObject
        {
            get;
            set;
        }

        public void Attach(PerspexObject associatedObject)
        {
            // TODO: Check for design mode
            if ((associatedObject != AssociatedObject) /*&& !Windows.ApplicationModel.DesignMode.DesignModeEnabled*/)
            {
                AssociatedObject = associatedObject;
                var fe = AssociatedObject as Control;
                if (fe != null)
                {
                    fe.PointerPressed += fe_PointerPressed;
                    fe.PointerReleased += fe_PointerReleased;
                }
            }
        }

        Control parent = null;
        Point prevPoint;
        //int pointerId = -1;
        void fe_PointerPressed(object sender, PointerPressEventArgs e)
        {
            var fe = AssociatedObject as Control;
            parent = (Control)fe.Parent;

            if (!(fe.RenderTransform is TranslateTransform))
                fe.RenderTransform = new TranslateTransform();
            prevPoint = e.GetPosition(parent);
            parent.PointerMoved += move;
            //pointerId = (int)e.Pointer.PointerId;
        }

        private void Parent_PointerMoved(object sender, PointerEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void move(object o, PointerEventArgs args)
        {
            //if (args.Pointer.PointerId != pointerId)
            //    return;

            var fe = AssociatedObject as Control;
            var pos = args.GetPosition(parent);
            var tr = (TranslateTransform)fe.RenderTransform;
            tr.X += pos.X - prevPoint.X;
            tr.Y += pos.Y - prevPoint.Y;
            prevPoint = pos;
        }
        void fe_PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            var fe = AssociatedObject as Control;
            //if (e.Pointer.PointerId != pointerId)
            //    return;
            parent.PointerMoved -= move;
            //pointerId = -1;
        }
        public void Detach()
        {
            var fe = AssociatedObject as Control;
            if (fe != null)
            {
                fe.PointerPressed -= fe_PointerPressed;
                fe.PointerReleased -= fe_PointerReleased;
            }
            parent = null;
            AssociatedObject = null;
        }
    }
}

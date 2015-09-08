using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Controls.Primitives
{

    public interface IScrollInfoBase
    {
        /// <summary>
        /// ScrollOwner is the container that controls any scrollbars, headers, etc... that are dependant 
        /// on this IScrollInfo's properties.  Implementers of IScrollInfo should call InvalidateScrollInfo() 
        /// on this object when properties change.
        /// </summary> 
        ScrollViewer ScrollOwner { get; set; }

        Rect MakeVisible(Visual visual, Rect rectangle);
    }

    public interface IVerticalScrollInfo : IScrollInfoBase
    {
        /// <summary> 
        /// VerticalOffset is the vertical offset into the scrolled content that represents the first unit visible.
        /// </summary>
        double VerticalOffset { get; set; }

        /// <summary> 
        /// ExtentHeight contains the full vertical range of the scrolled content.
        /// </summary> 
        double ExtentHeight { get; }

        /// <summary>
        /// ViewportHeight contains the currently visible vertical range of the scrolled content. 
        /// </summary>
        double ViewportHeight { get; }

        /// <summary>
        /// This property indicates to the IScrollInfo whether or not it can scroll in the vertical given dimension.
        /// </summary> 
        bool CanVerticallyScroll { get; set; }

        void LineDown();

        void LineUp();

        void MouseWheelDown();

        void MouseWheelUp();

        void PageDown();

        void PageUp();
    }

    public interface IHorizontalScrollInfo : IScrollInfoBase
    {
        /// <summary>
        /// ExtentWidth contains the full horizontal range of the scrolled content. 
        /// </summary>
        double ExtentWidth { get; }

        /// <summary> 
        /// ViewportWidth contains the currently visible horizontal range of the scrolled content.
        /// </summary>
        double ViewportWidth { get; }

        /// <summary>
        /// HorizontalOffset is the horizontal offset into the scrolled content that represents the first unit visible.
        /// </summary> 
        double HorizontalOffset { get; set; }

        /// <summary> 
        /// This property indicates to the IScrollInfo whether or not it can scroll in the horizontal given dimension. 
        /// </summary>
        bool CanHorizontallyScroll { get; set; }

        void LineLeft();

        void LineRight();

        void MouseWheelLeft();

        void MouseWheelRight();

        void PageLeft();

        void PageRight();
    }

    public interface IScrollInfo : IHorizontalScrollInfo, IVerticalScrollInfo
    {
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using System;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Defines the Panel used by the <see cref="LoopingSelector"/>
    /// </summary>
    public sealed class LoopingPanel : Panel, ILogicalScrollable
    {
        public LoopingPanel(LoopingSelector owner)
        {
            _owner = owner;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            //It's assumed here that availableSize will have finite values
            //If used in the DatePickerPresenter or TimePickerPresenter, this
            //is met and works fine. If used elsewhere, ensure this is met
            //Width is required for the Items & height is required for the viewportsize
            if (double.IsInfinity(availableSize.Width) || double.IsInfinity(availableSize.Height))
                throw new InvalidOperationException("LoopingPanel needs finite bounds");

            //For the measure pass, remember we only have a subset of the total items 
            //available. So we need to ask the LoopingSelector for it's Item count
            //return a size based on the extent of all items

            var itmHgt = _owner.ItemHeight;
            var itemCt = _owner.ItemCount;
            var children = Children;

            var itemWid = availableSize.Width;
            for (int i = 0; i < children.Count; i++)
            {
                //Ensure we have a proper size when measuring
                (children[i] as LoopingSelectorItem).Width = itemWid;
                (children[i] as LoopingSelectorItem).Height = itmHgt;
                children[i].Measure(availableSize);
            }

            var hei = itmHgt * itemCt;

            if (_owner.ShouldLoop)
            {
                //WinUI preps for somewhere around 1000 items? in loop mode, then positions the offset in the middle
                //based on the SelectedItem index, that way scrolling is enabled in both directions
                //Here we prep for 10 * ItemCount & position in middle to start
                _extent = new Size(0, 10 * (itmHgt * itemCt) + (availableSize.Height - itmHgt));
                _viewport = new Size(0, availableSize.Height);

                if (!_hasInitLoop)
                {
                    var selIndex = _owner.SelectedIndex;
                    selIndex = selIndex == -1 ? 0 : selIndex;

                    //We know we are measuring for 10x items,
                    //so our init index is the selecteditems' index * 5
                    if (double.IsNaN(initOffset))
                    {
                        _offset = new Vector(0, (selIndex * itmHgt) * 5);
                    }
                    else
                    {
                        _offset = new Vector(0, initOffset);
                        initOffset = double.NaN;
                    }
                    _hasInitLoop = true;
                }
            }
            else
            {
                //SelectedItem is in the middle of the LoopingSelector, so we need to account for that
                //so all items can end up in this position
                _extent = new Size(0, hei + (availableSize.Height - itmHgt));
                _viewport = new Size(0, availableSize.Height);

                if (!double.IsNaN(initOffset))
                {
                    _offset = new Vector(0, initOffset);
                    initOffset = double.NaN;
                }
            }

            //Total items visible, whether fully or partially visible
            _totalItemsInViewport = (int)Math.Ceiling(_viewport.Height / itmHgt);

            RaiseScrollInvalidated(null);
            return _extent;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_owner == null || Children.Count == 0)
                return base.ArrangeOverride(finalSize);

            var itemWid = finalSize.Width;
            var itemHgt = _owner.ItemHeight;
            var initY = (finalSize.Height / 2.0) - (itemHgt / 2.0);
            var children = Children;
            var offY = Offset.Y;

            //Item arranging: SelectedItem is placed in the middle of the viewport
            //There are _totalItemsInViewport above & below
            //In default behavior of Date/Time Picker, 9 items are visible in viewport,
            //so 19 items are arrange, 9 above, 9 below, and the selected item inbetween
            //The exception is if we are NOT looping & we're near the ends of the available
            //items, then we only have what's left before/after selecteditem visible
            if (_owner.ShouldLoop)
            {
                //Correct our starting Y for not starting in the middle when looping
                initY -= (itemHgt * _totalItemsInViewport);

                //Sort of limitation when looping, we just place the containers & 
                //swap the content. With logical scrolling enabled, and the way its
                //handled in Avalonia currently (both pointer wheel & gestures)
                //you won't notice a difference. Though this may need updating later
                //if this changes
                Rect rc;
                for (int i = 0; i < children.Count; i++)
                {
                    rc = new Rect(0, initY, itemWid, itemHgt);
                    children[i].Arrange(rc);
                    initY += itemHgt;
                }
            }
            else
            {
                int firstIndex = Math.Max(0, _owner.SelectedIndex - _totalItemsInViewport);
                Rect rc;
                for (int i = 0; i < children.Count; i++)
                {
                    rc = new Rect(0, initY - offY + firstIndex * itemHgt, itemWid, itemHgt);
                    children[i].Arrange(rc);
                    initY += itemHgt;
                }
            }

            return new Size(itemWid, _extent.Height);
        }

        public bool CanHorizontallyScroll { get => false; set => _ = value; }

        public bool CanVerticallyScroll { get => true; set => _ = value; }

        public bool IsLogicalScrollEnabled => true;

        public Size ScrollSize => new Size(0, _owner?.ItemHeight ?? 32);

        //4 items
        public Size PageScrollSize => new Size(0, _owner?.ItemHeight * 4 ?? 128);

        public Size Extent => _extent;

        public Vector Offset
        {
            get => _offset;
            set
            {
                //If we try setting the offset before we've intialized
                //store the value now & set it when MeasureOverride is called
                if (Extent.Height == 0)
                {
                    initOffset = value.Y;
                    return;
                }

                var old = _offset;
                _offset = value;

                if (Children.Count == 0)
                    return;

                _owner.SetSelectedIndexFromOffset(value.Y);

                var itemHgt = _owner.ItemHeight;
                var totalItemCount = _owner.ItemCount;
                var initY = (Bounds.Height / 2) - (itemHgt / 2);

                if (_owner.ShouldLoop)
                {
                    //If we're looping, we need to detect when we're approaching 
                    //the min/max bounds of the scrollviewer & reset to the otherside
                    //to make sure we always have scrolling 
                    //To do this, since we plan for 10x total items, we move if we're
                    //in the first or last "block" of items & return it to somewhere near the middle
                    if (value.Y > old.Y) //Scrolling Down
                    {
                        var extentOne = totalItemCount * itemHgt;
                        var scrollableHeight = (_extent.Height - _viewport.Height);
                        if (value.Y >= scrollableHeight - extentOne)
                            _offset = new Vector(0, value.Y - (extentOne * 5));
                    }
                    else if (value.Y < old.Y) //Scrolling Up
                    {
                        var extentOne = totalItemCount * itemHgt;
                        var scrollableHeight = (_extent.Height - _viewport.Height);
                        if (value.Y < extentOne)
                            _offset = new Vector(0, value.Y + (extentOne * 5));
                    }

                    firstIndex = _owner.SelectedIndex - _totalItemsInViewport;
                }
                else
                {

                    var numItemsAboveSelected = (int)Math.Ceiling(initY / itemHgt);
                    int logicalOffset = (int)(_offset.Y / itemHgt);

                    firstIndex = Math.Max(0, Math.Min(logicalOffset - numItemsAboveSelected, totalItemCount));

                    //When not looping, we actually move the containers, so if we get one
                    //out of bounds, recycle it to the other side
                    if (_offset.Y > old.Y)
                    {
                        var recycleThreshold = initY - (_totalItemsInViewport * itemHgt);

                        //ScrollDown
                        var ct = Children.Count;
                        for (int i = ct - 1; i >= 0; i--)
                        {
                            if (Children[i].Bounds.Bottom <= recycleThreshold)
                            {
                                Children.Move(i, ct - 1);
                            }
                        }
                    }
                    else if (_offset.Y < old.Y)
                    {
                        var recycleThreshold = (initY + itemHgt) + (_totalItemsInViewport * itemHgt);
                        //ScrollUp
                        var ct = Children.Count;
                        var bottom = Bounds.Height;
                        for (int i = ct - 1; i >= 0; i--)
                        {
                            if (Children[i].Bounds.Top >= recycleThreshold)
                            {
                                Children.Move(i, 0);
                            }
                        }
                    }
                }                               

                RaiseScrollInvalidated(EventArgs.Empty);
                InvalidateArrange();
            }
        }

        public Size Viewport => _viewport;

        //Not used
        public bool BringIntoView(IControl target, Rect targetRect)
        {
            return false;
        }

        //Not used
        public IControl GetControlInDirection(NavigationDirection direction, IControl from)
        {
            return null;
        }

        public void RaiseScrollInvalidated(EventArgs e)
        {
            ScrollInvalidated?.Invoke(this, e);
        }

        private double initOffset = double.NaN;
        int firstIndex = 0;
        private LoopingSelector _owner;
        private Size _extent;
        private Size _viewport;
        private Vector _offset;
        private bool _hasInitLoop;
        private int _totalItemsInViewport;

        public event EventHandler ScrollInvalidated;
    }
}

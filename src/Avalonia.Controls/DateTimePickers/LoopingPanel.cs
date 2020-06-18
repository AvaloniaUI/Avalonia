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

            //When not looping, currentSet will always be 0
            //When looping, we measure for 10x _owner.ItemCount, so we need to figure out
            //which "set" of items we're in based on the offset so we know where to properly
            //place items
            var singleExtent = _owner.ItemCount * itemHgt;
            var currentSet = Math.Truncate(offY / singleExtent);

            int selIndex = _owner.SelectedIndex;
            selIndex = selIndex == -1 ? 0 : selIndex;

            //When looping, the selected item should always be the middle item, equivalent in index
            //to _totalItemsInViewport
            //When not looping, if we're near the beginning of the list, our first item may be less than
            //_totalItemsInViewport, so we need to make sure in that case to make the selected item, the
            //actual selected index
            var childIndexOfSelected = _totalItemsInViewport;
            if (!_owner.ShouldLoop && selIndex < _totalItemsInViewport)
                childIndexOfSelected = selIndex;

            //Our selected container forms our "anchor" and all other containers are placed around this
            IControl containerOfSelected = children[childIndexOfSelected];

            //Then we need to know how many containers are above and below the selected item
            //Should be _totalItemsInViewport for both, unless not looping & near items start
            //# containers above is just the index of the selected item container
            var numContainersAboveSelected = childIndexOfSelected;

            //Move initY to where we actually want to start placing the items
            initY += (singleExtent * currentSet) + (selIndex * itemHgt);

            //We first arrange the selected item
            Rect rc = new Rect(0, initY - offY, itemWid, itemHgt);
            containerOfSelected.Arrange(rc);

            //Arrange all items above
            var prevY = initY - itemHgt;
            for (int i = numContainersAboveSelected - 1; i >= 0; i--)
            {
                rc = new Rect(0, prevY - offY, itemWid, itemHgt);
                children[i].Arrange(rc);
                prevY -= itemHgt;
            }

            //Finally arrange all items below
            var nextY = initY + itemHgt;
            for (int i = childIndexOfSelected + 1; i < children.Count; i++)
            {
                rc = new Rect(0, nextY - offY, itemWid, itemHgt);
                children[i].Arrange(rc);
                nextY += itemHgt;
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
                if (Extent.Height == 0)
                {
                    initOffset = value.Y;
                    return;
                }

                var old = _offset.Y;
                _offset = value;

                if (Children.Count == 0)
                    return;

                var itemHgt = _owner.ItemHeight;
                var totalItemCount = _owner.ItemCount;

                if (_owner.ShouldLoop)
                {
                    //If we're looping, we need to detect when we're approaching 
                    //the min/max bounds of the scrollviewer & reset to the otherside
                    //to make sure we always have scrolling 
                    //To do this, since we plan for 10x total items, we move if we're
                    //in the first or last "block" of items & return it to somewhere near the middle
                    if (value.Y > old) //Scrolling Down
                    {
                        var extentOne = totalItemCount * itemHgt;
                        var scrollableHeight = (_extent.Height - _viewport.Height);
                        if (value.Y >= scrollableHeight - extentOne)
                        {
                            _offset = new Vector(0, value.Y - (extentOne * 5));
                            old = old - (extentOne * 5);
                        }
                    }
                    else if (value.Y < old) //Scrolling Up
                    {
                        var extentOne = totalItemCount * itemHgt;
                        if (value.Y < extentOne)
                        {
                            _offset = new Vector(0, value.Y + (extentOne * 5));
                            old = old + (extentOne * 5);
                        }
                    }
                }

                _owner.SetSelectedIndexFromOffset(old, _offset.Y);

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
        private LoopingSelector _owner;
        private Size _extent;
        private Size _viewport;
        private Vector _offset;
        private bool _hasInitLoop;
        private int _totalItemsInViewport;

        public event EventHandler ScrollInvalidated;
    }
}

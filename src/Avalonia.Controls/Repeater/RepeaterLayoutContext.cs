// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    internal class RepeaterLayoutContext : VirtualizingLayoutContext
    {
        private readonly ItemsRepeater _owner;

        public RepeaterLayoutContext(ItemsRepeater owner)
        {
            _owner = owner;
        }

        protected override Point LayoutOriginCore
        {
            get => _owner.LayoutOrigin;
            set => _owner.LayoutOrigin = value;
        }

        protected override object LayoutStateCore
        {
            get => _owner.LayoutState;
            set => _owner.LayoutState = value;
        }

        protected override int RecommendedAnchorIndexCore
        {
            get
            {
                int anchorIndex = -1;
                var anchor = _owner.SuggestedAnchor;
                if (anchor != null)
                {
                    anchorIndex = _owner.GetElementIndex(anchor);
                }

                return anchorIndex;
            }
        }

        protected override int ItemCountCore() => _owner.ItemsSourceView?.Count ?? 0;

        protected override ILayoutable GetOrCreateElementAtCore(int index, ElementRealizationOptions options)
        {
            return _owner.GetElementImpl(
                index,
                (options & ElementRealizationOptions.ForceCreate) != 0,
                (options & ElementRealizationOptions.SuppressAutoRecycle) != 0);
        }

        protected override object GetItemAtCore(int index) => _owner.ItemsSourceView.GetAt(index);

        protected override void RecycleElementCore(ILayoutable element) => _owner.ClearElementImpl((IControl)element);

        protected override Rect RealizationRectCore() => _owner.RealizationWindow;
    }
}

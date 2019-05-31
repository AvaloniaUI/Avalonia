using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Repeaters
{
    internal sealed class ViewManager
    {
        private const int FirstRealizedElementIndexDefault = int.MaxValue;
        private const int LastRealizedElementIndexDefault = int.MinValue;

        private readonly ItemsRepeater _owner;
        private readonly List<PinnedElementInfo> _pinnedPool = new List<PinnedElementInfo>();
        private readonly UniqueIdElementPool _resetPool;
        private IControl _lastFocusedElement;
        private bool _isDataSourceStableResetPending;
        private ElementFactoryGetArgs _elementFactoryGetArgs;
        private ElementFactoryRecycleArgs _elementFactoryRecycleArgs;
        private int _firstRealizedElementIndexHeldByLayout = FirstRealizedElementIndexDefault;
        private int _lastRealizedElementIndexHeldByLayout = LastRealizedElementIndexDefault;
        private bool _eventsSubscribed;

        public ViewManager(ItemsRepeater owner)
        {
            _owner = owner;
            _resetPool = new UniqueIdElementPool(owner);
        }

        public IControl GetElement(int index, bool forceCreate, bool suppressAutoRecycle)
        {
            var element = forceCreate ? null : GetElementIfAlreadyHeldByLayout(index);
            if (element == null)
            {
                // check if this is the anchor made through repeater in preparation 
                // for a bring into view.
                var madeAnchor = _owner.MadeAnchor;
                if (madeAnchor != null)
                {
                    var anchorVirtInfo = ItemsRepeater.TryGetVirtualizationInfo(madeAnchor);
                    if (anchorVirtInfo.Index == index)
                    {
                        element = madeAnchor;
                    }
                }
            }
            if (element == null) { element = GetElementFromUniqueIdResetPool(index); };
            if (element == null) { element = GetElementFromPinnedElements(index); }
            if (element == null) { element = GetElementFromElementFactory(index); }

            var virtInfo = ItemsRepeater.TryGetVirtualizationInfo(element);
            if (suppressAutoRecycle)
            {
                virtInfo.AutoRecycleCandidate = false;
            }
            else
            {
                virtInfo.AutoRecycleCandidate = true;
                virtInfo.KeepAlive = true;
            }

            return element;
        }

        public void ClearElement(IControl element, bool isClearedDueToCollectionChange)
        {
            var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
            var index = virtInfo.Index;
            bool cleared =
                ClearElementToUniqueIdResetPool(element, virtInfo) ||
                ClearElementToPinnedPool(element, virtInfo, isClearedDueToCollectionChange);

            if (!cleared)
            {
                ClearElementToElementFactory(element);
            }

            // Both First and Last indices need to be valid or default.
            if (index == _firstRealizedElementIndexHeldByLayout && index == _lastRealizedElementIndexHeldByLayout)
            {
                // First and last were pointing to the same element and that is going away.
                InvalidateRealizedIndicesHeldByLayout();
            }
            else if (index == _firstRealizedElementIndexHeldByLayout)
            {
                // The FirstElement is going away, shrink the range by one.
                ++_firstRealizedElementIndexHeldByLayout;
            }
            else if (index == _lastRealizedElementIndexHeldByLayout)
            {
                // Last element is going away, shrink the range by one at the end.
                --_lastRealizedElementIndexHeldByLayout;
            }
            else
            {
                // Index is either outside the range we are keeping track of or inside the range.
                // In both these cases, we just keep the range we have. If this clear was due to 
                // a collection change, then in the CollectionChanged event, we will invalidate these guys.
            }
        }

        public void ClearElementToElementFactory(IControl element)
        {
            var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
            var clearedIndex = virtInfo.Index;
            _owner.OnElementClearing(element);

            if (_elementFactoryRecycleArgs == null)
            {
                // Create one.
                _elementFactoryRecycleArgs = new ElementFactoryRecycleArgs();
            }

            var context = _elementFactoryRecycleArgs;
            context.Element = element;
            context.Parent = _owner;

            _owner.ItemTemplateShim.RecycleElement(context);

            context.Element = null;
            context.Parent = null;

            virtInfo.MoveOwnershipToElementFactory();
            //_phaser.StopPhasing(element, virtInfo);
            if (_lastFocusedElement == element)
            {
                // Focused element is going away. Remove the tracked last focused element
                // and pick a reasonable next focus if we can find one within the layout 
                // realized elements.
                MoveFocusFromClearedIndex(clearedIndex);
            }

        }

        private void MoveFocusFromClearedIndex(int clearedIndex)
        {
            IControl focusedChild = null;
            var focusCandidate = FindFocusCandidate(clearedIndex, focusedChild);
            if (focusCandidate != null)
            {
                //var focusState = _lastFocusedElement?.FocusState ?? FocusState.Programmatic;

                // If the last focused element has focus, use its focus state, if not use programmatic.
                //focusState = focusState == FocusState.Unfocused ? FocusState.Programmatic : focusState;
                focusCandidate.Focus();

                _lastFocusedElement = focusedChild;
                // Add pin to hold the focused element.
                UpdatePin(focusedChild, true /* addPin */);
            }
            else
            {
                // We could not find a candiate.
                _lastFocusedElement = null;
            }
        }

        IControl FindFocusCandidate(int clearedIndex, IControl focusedChild)
        {
            // Walk through all the children and find elements with index before and after the cleared index.
            // Note that during a delete the next element would now have the same index.
            int previousIndex = int.MinValue;
            int nextIndex = int.MaxValue;
            IControl nextElement = null;
            IControl previousElement = null;

            foreach (var child in _owner.Children)
            {
                var virtInfo = ItemsRepeater.TryGetVirtualizationInfo(child);
                if (virtInfo?.IsHeldByLayout == true)
                {
                    int currentIndex = virtInfo.Index;
                    if (currentIndex < clearedIndex)
                    {
                        if (currentIndex > previousIndex)
                        {
                            previousIndex = currentIndex;
                            previousElement = child;
                        }
                    }
                    else if (currentIndex >= clearedIndex)
                    {
                        // Note that we use >= above because if we deleted the focused element, 
                        // the next element would have the same index now.
                        if (currentIndex < nextIndex)
                        {
                            nextIndex = currentIndex;
                            nextElement = child;
                        }
                    }
                }
            }

            // Find the next element if one exists, if not use the previous element.
            // If the container itself is not focusable, find a descendent that is.
            IControl focusCandidate = null;
            if (nextElement != null)
            {
                focusCandidate = nextElement as IControl;
                if (focusCandidate != null)
                {
                    ////var firstFocus = FocusManager.FindFirstFocusableElement(nextElement);

                    ////if (firstFocus != null)
                    ////{
                    ////    focusCandidate = firstFocus as IControl;
                    ////}
                }
            }

            if (focusCandidate == null && previousElement != null)
            {
                focusCandidate = previousElement as IControl;
                if (previousElement != null)
                {
                    ////var lastFocus = FocusManager.FindLastFocusableElement(previousElement);

                    ////if (lastFocus != null)
                    ////{
                    ////    focusCandidate = lastFocus as IControl;
                    ////}
                }
            }

            return focusCandidate;
        }

        public int GetElementIndex(VirtualizationInfo virtInfo)
        {
            if (virtInfo == null)
            {
                throw new ArgumentException("Element is not a child of this ItemsRepeater.");
            }

            return virtInfo.IsRealized || virtInfo.IsInUniqueIdResetPool ? virtInfo.Index : -1;
        }

        public void PrunePinnedElements()
        {
            EnsureEventSubscriptions();

            // Go through pinned elements and make sure they still have
            // a reason to be pinned.
            for (var i = 0; i < _pinnedPool.Count; ++i)
            {
                var elementInfo = _pinnedPool[i];
                var virtInfo = elementInfo.VirtualizationInfo;

                //MUX_ASSERT(virtInfo.Owner() == ElementOwner.PinnedPool);

                if (!virtInfo.IsPinned)
                {
                    _pinnedPool.RemoveAt(i);
                    --i;

                    // Pinning was the only thing keeping this element alive.
                    ClearElementToElementFactory(elementInfo.PinnedElement);
                }
            }
        }

        public void UpdatePin(IControl element, bool addPin)
        {
            var parent = element.VisualParent;
            var child = (IVisual)element;

            while (parent != null)
            {
                if (parent is ItemsRepeater repeater)
                {
                    var virtInfo = ItemsRepeater.GetVirtualizationInfo((IControl)child);
                    if (virtInfo.IsRealized)
                    {
                        if (addPin)
                        {
                            virtInfo.AddPin();
                        }
                        else if (virtInfo.IsPinned)
                        {
                            if (virtInfo.RemovePin() == 0)
                            {
                                // ElementFactory is invoked during the measure pass.
                                // We will clear the element then.
                                repeater.InvalidateMeasure();
                            }
                        }
                    }
                }

                child = parent;
                parent = child.VisualParent;
            }
        }

        public void OnItemsSourceChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            // Note: For items that have been removed, the index will not be touched. It will hold
            // the old index before it was removed. It is not valid anymore.
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        var newIndex = args.NewStartingIndex;
                        var newCount = args.NewItems.Count;
                        EnsureFirstLastRealizedIndices();
                        if (newIndex <= _lastRealizedElementIndexHeldByLayout)
                        {
                            _lastRealizedElementIndexHeldByLayout += newCount;
                            foreach (var element in _owner.Children)
                            {
                                var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
                                var dataIndex = virtInfo.Index;

                                if (virtInfo.IsRealized && dataIndex >= newIndex)
                                {
                                    UpdateElementIndex(element, virtInfo, dataIndex + newCount);
                                }
                            }
                        }
                        else
                        {
                            // Indices held by layout are not affected
                            // We could still have items in the pinned elements that need updates. This is usually a very small vector.
                            for (var i = 0; i < _pinnedPool.Count; ++i)
                            {
                                var elementInfo = _pinnedPool[i];
                                var virtInfo = elementInfo.VirtualizationInfo;
                                var dataIndex = virtInfo.Index;

                                if (virtInfo.IsRealized && dataIndex >= newIndex)
                                {
                                    var element = elementInfo.PinnedElement;
                                    UpdateElementIndex(element, virtInfo, dataIndex + newCount);
                                }
                            }
                        }
                        break;
                    }

                case NotifyCollectionChangedAction.Replace:
                    {
                        // Requirement: oldStartIndex == newStartIndex. It is not a replace if this is not true.
                        // Two cases here
                        // case 1: oldCount == newCount 
                        //         indices are not affected. nothing to do here.  
                        // case 2: oldCount != newCount
                        //         Replaced with less or more items. This is like an insert or remove
                        //         depending on the counts.
                        var oldStartIndex = args.OldStartingIndex;
                        var newStartingIndex = args.NewStartingIndex;
                        var oldCount = args.OldItems.Count;
                        var newCount = args.NewItems.Count;
                        if (oldStartIndex != newStartingIndex)
                        {
                            throw new NotSupportedException("Replace is only allowed with OldStartingIndex equals to NewStartingIndex.");
                        }

                        if (oldCount == 0)
                        {
                            throw new NotSupportedException("Replace notification with args.OldItemsCount value of 0 is not allowed. Use Insert action instead.");
                        }

                        if (newCount == 0)
                        {
                            throw new NotSupportedException("Replace notification with args.NewItemCount value of 0 is not allowed. Use Remove action instead.");
                        }

                        int countChange = newCount - oldCount;
                        if (countChange != 0)
                        {
                            // countChange > 0 : countChange items were added
                            // countChange < 0 : -countChange  items were removed
                            foreach (var element in _owner.Children)
                            {
                                var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
                                var dataIndex = virtInfo.Index;

                                if (virtInfo.IsRealized)
                                {
                                    if (dataIndex >= oldStartIndex + oldCount)
                                    {
                                        UpdateElementIndex(element, virtInfo, dataIndex + countChange);
                                    }
                                }
                            }

                            EnsureFirstLastRealizedIndices();
                            _lastRealizedElementIndexHeldByLayout += countChange;
                        }
                        break;
                    }

                case NotifyCollectionChangedAction.Remove:
                    {
                        var oldStartIndex = args.OldStartingIndex;
                        var oldCount = args.OldItems.Count;
                        foreach (var element in _owner.Children)
                        {
                            var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
                            var dataIndex = virtInfo.Index;

                            if (virtInfo.IsRealized)
                            {
                                if (virtInfo.AutoRecycleCandidate && oldStartIndex <= dataIndex && dataIndex < oldStartIndex + oldCount)
                                {
                                    // If we are doing the mapping, remove the element who's data was removed.
                                    _owner.ClearElementImpl(element);
                                }
                                else if (dataIndex >= (oldStartIndex + oldCount))
                                {
                                    UpdateElementIndex(element, virtInfo, dataIndex - oldCount);
                                }
                            }
                        }

                        InvalidateRealizedIndicesHeldByLayout();
                        break;
                    }

                case NotifyCollectionChangedAction.Reset:
                    if (_owner.ItemsSourceView.HasKeyIndexMapping)
                    {
                        _isDataSourceStableResetPending = true;
                    }

                    // Walk through all the elements and make sure they are cleared, they will go into
                    // the stable id reset pool.
                    foreach (var element in _owner.Children)
                    {
                        var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
                        if (virtInfo.IsRealized && virtInfo.AutoRecycleCandidate)
                        {
                            _owner.ClearElementImpl(element);
                        }
                    }

                    InvalidateRealizedIndicesHeldByLayout();
                    break;
            }
        }

        private void EnsureFirstLastRealizedIndices()
        {
            if (_firstRealizedElementIndexHeldByLayout == FirstRealizedElementIndexDefault)
            {
                // This will ensure that the indexes are updated.
                GetElementIfAlreadyHeldByLayout(0);
            }
        }

        public void OnLayoutChanging()
        {
            if (_owner.ItemsSourceView?.HasKeyIndexMapping == true)
            {
                _isDataSourceStableResetPending = true;
            }
        }

        public void OnOwnerArranged()
        {
            if (_isDataSourceStableResetPending)
            {
                _isDataSourceStableResetPending = false;

                foreach (var entry in _resetPool)
                {
                    // TODO: Task 14204306: ItemsRepeater: Find better focus candidate when focused element is deleted in the ItemsSource.
                    // Focused element is getting cleared. Need to figure out semantics on where
                    // focus should go when the focused element is removed from the data collection.
                    ClearElement(entry.Value, true /* isClearedDueToCollectionChange */);
                }

                _resetPool.Clear();
            }
        }

        // We optimize for the case where index is not realized to return null as quickly as we can.
        // Flow layouts manage containers on their own and will never ask for an index that is already realized.
        // If an index that is realized is requested by the layout, we unfortunately have to walk the
        // children. Not ideal, but a reasonable default to provide consistent behavior between virtualizing
        // and non-virtualizing hosts.
        private IControl GetElementIfAlreadyHeldByLayout(int index)
        {
            IControl element = null;

            bool cachedFirstLastIndicesInvalid = _firstRealizedElementIndexHeldByLayout == FirstRealizedElementIndexDefault;
            //MUX_ASSERT(!cachedFirstLastIndicesInvalid || m_lastRealizedElementIndexHeldByLayout == LastRealizedElementIndexDefault);

            bool isRequestedIndexInRealizedRange = (_firstRealizedElementIndexHeldByLayout <= index && index <= _lastRealizedElementIndexHeldByLayout);

            if (cachedFirstLastIndicesInvalid || isRequestedIndexInRealizedRange)
            {
                // Both First and Last indices need to be valid or default.
                //MUX_ASSERT((m_firstRealizedElementIndexHeldByLayout == FirstRealizedElementIndexDefault && m_lastRealizedElementIndexHeldByLayout == LastRealizedElementIndexDefault) ||
                //    (m_firstRealizedElementIndexHeldByLayout != FirstRealizedElementIndexDefault && m_lastRealizedElementIndexHeldByLayout != LastRealizedElementIndexDefault));

                foreach (var child in _owner.Children)
                {
                    var virtInfo = ItemsRepeater.TryGetVirtualizationInfo(child);
                    if (virtInfo?.IsHeldByLayout == true)
                    {
                        // Only give back elements held by layout. If someone else is holding it, they will be served by other methods.
                        int childIndex = virtInfo.Index;
                        _firstRealizedElementIndexHeldByLayout = Math.Min(_firstRealizedElementIndexHeldByLayout, childIndex);
                        _lastRealizedElementIndexHeldByLayout = Math.Max(_lastRealizedElementIndexHeldByLayout, childIndex);
                        if (virtInfo.Index == index)
                        {
                            element = child;
                            // If we have valid first/last indices, we don't have to walk the rest, but if we 
                            // do not, then we keep walking through the entire children collection to get accurate
                            // indices once.
                            if (!cachedFirstLastIndicesInvalid)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return element;
        }

        private IControl GetElementFromUniqueIdResetPool(int index)
        {
            IControl element = null;
            // See if you can get it from the reset pool.
            if (_isDataSourceStableResetPending)
            {
                element = _resetPool.Remove(index);
                if (element != null)
                {
                    // Make sure that the index is updated to the current one
                    var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
                    virtInfo.MoveOwnershipToLayoutFromUniqueIdResetPool();
                    UpdateElementIndex(element, virtInfo, index);
                }
            }

            return element;
        }

        private IControl GetElementFromPinnedElements(int index)
        {
            IControl element = null;

            // See if you can find something among the pinned elements.
            for (var i = 0; i < _pinnedPool.Count; ++i)
            {
                var elementInfo = _pinnedPool[i];
                var virtInfo = elementInfo.VirtualizationInfo;

                if (virtInfo.Index == index)
                {
                    _pinnedPool.RemoveAt(i);
                    element = elementInfo.PinnedElement;
                    elementInfo.VirtualizationInfo.MoveOwnershipToLayoutFromPinnedPool();
                    break;
                }
            }

            return element;
        }

        private IControl GetElementFromElementFactory(int index)
        {
            // The view generator is the provider of last resort.

            var itemTemplateFactory = _owner.ItemTemplateShim;
            if (itemTemplateFactory == null)
            {
                // If no ItemTemplate was provided, use a default 
                var factory = FuncDataTemplate.Default;
                _owner.ItemTemplate = factory;
                itemTemplateFactory = _owner.ItemTemplateShim;
            }

            var data = _owner.ItemsSourceView.GetAt(index);

            if (_elementFactoryGetArgs == null)
            {
                // Create one.
                _elementFactoryGetArgs = new ElementFactoryGetArgs();
            }

            var args = _elementFactoryGetArgs;
            args.Data = data;
            args.Parent = _owner;
            args.Index= index;

            var element = itemTemplateFactory.GetElement(args);

            args.Data = null;
            args.Parent = null;

            var virtInfo = ItemsRepeater.TryGetVirtualizationInfo(element);
            if (virtInfo == null)
            {
                virtInfo = ItemsRepeater.CreateAndInitializeVirtualizationInfo(element);
            }

            // Prepare the element
            // If we are phasing, run phase 0 before setting DataContext. If phase 0 is not 
            // run before setting DataContext, when setting DataContext all the phases will be
            // run in the OnDataContextChanged handler in code generated by the xaml compiler (code-gen).
            var extension = false; ////CachedVisualTreeHelpers.GetDataTemplateComponent(element);
            if (extension)
            {
                ////// Clear out old data. 
                ////extension.Recycle();
                ////int nextPhase = VirtualizationInfo.PhaseReachedEnd;
                ////// Run Phase 0
                ////extension.ProcessBindings(data, index, 0 /* currentPhase */, nextPhase);

                ////// Setup phasing information, so that Phaser can pick up any pending phases left.
                ////// Update phase on virtInfo. Set data and templateComponent only if x:Phase was used.
                ////virtInfo.UpdatePhasingInfo(nextPhase, nextPhase > 0 ? data : null, nextPhase > 0 ? extension : null);
            }
            else
            {
                // Set data context only if no x:Bind was used. ie. No data template component on the root.
                element.DataContext = data;
            }

            virtInfo.MoveOwnershipToLayoutFromElementFactory(
                index,
                /* uniqueId: */
                _owner.ItemsSourceView.HasKeyIndexMapping ?
                _owner.ItemsSourceView.KeyFromIndex(index) :
                string.Empty);

            // The view generator is the only provider that prepares the element.
            var repeater = _owner;

            // Add the element to the children collection here before raising OnElementPrepared so 
            // that handlers can walk up the tree in case they want to find their IndexPath in the 
            // nested case.
            var children = repeater.Children;
            if (element.VisualParent != repeater)
            {
                children.Add(element);
            }

            ////repeater.AnimationManager.OnElementPrepared(element);
            repeater.OnElementPrepared(element, index);
            ////_phaser.PhaseElement(element, virtInfo);

            // Update realized indices
            _firstRealizedElementIndexHeldByLayout = Math.Min(_firstRealizedElementIndexHeldByLayout, index);
            _lastRealizedElementIndexHeldByLayout = Math.Max(_lastRealizedElementIndexHeldByLayout, index);

            return element;
        }

        private bool ClearElementToUniqueIdResetPool(IControl element, VirtualizationInfo virtInfo)
        {
            if (_isDataSourceStableResetPending)
            {
                _resetPool.Add(element);
                virtInfo.MoveOwnershipToUniqueIdResetPoolFromLayout();
            }

            return _isDataSourceStableResetPending;
        }

        private bool ClearElementToAnimator(IControl element, VirtualizationInfo virtInfo)
        {
            return false;
            ////bool cleared = _owner.AnimationManager.ClearElement(element);
            ////if (cleared)
            ////{
            ////    int clearedIndex = virtInfo.Index;
            ////    virtInfo.MoveOwnershipToAnimator();
            ////    if (_lastFocusedElement == element)
            ////    {
            ////        // Focused element is going away. Remove the tracked last focused element
            ////        // and pick a reasonable next focus if we can find one within the layout 
            ////        // realized elements.
            ////        MoveFocusFromClearedIndex(clearedIndex);
            ////    }
            ////}
            ////return cleared;
        }

        private bool ClearElementToPinnedPool(IControl element, VirtualizationInfo virtInfo, bool isClearedDueToCollectionChange)
        {
            if (_isDataSourceStableResetPending)
            {
                _resetPool.Add(element);
                virtInfo.MoveOwnershipToUniqueIdResetPoolFromLayout();
            }

            return _isDataSourceStableResetPending;
        }

        private void UpdateFocusedElement()
        {
            IControl focusedElement = null;

            var child = FocusManager.Instance.Current;

            if (child != null)
            {
                var parent = child.VisualParent;
                var owner = _owner;

                // Find out if the focused element belongs to one of our direct
                // children.
                while (parent != null)
                {
                    if (parent is ItemsRepeater repeater)
                    {
                        var element = child as IControl;
                        if (repeater == owner && ItemsRepeater.GetVirtualizationInfo(element).IsRealized)
                        {
                            focusedElement = element;
                        }

                        break;
                    }

                    child = parent as IInputElement;
                    parent = child.VisualParent;
                }
            }

            // If the focused element has changed,
            // we need to unpin the old one and pin the new one.
            if (_lastFocusedElement != focusedElement)
            {
                if (_lastFocusedElement != null)
                {
                    UpdatePin(_lastFocusedElement, false /* addPin */);
                }

                if (focusedElement != null)
                {
                    UpdatePin(focusedElement, true /* addPin */);
                }

                _lastFocusedElement = focusedElement;
            }
        }

        private void OnFocusChanged(object sender, RoutedEventArgs e) => UpdateFocusedElement();

        private void EnsureEventSubscriptions()
        {
            if (!_eventsSubscribed)
            {
                _owner.GotFocus += OnFocusChanged;
                _owner.LostFocus += OnFocusChanged;
            }
        }

        private void UpdateElementIndex(IControl element, VirtualizationInfo virtInfo, int index)
        {
            var oldIndex = virtInfo.Index;
            if (oldIndex != index)
            {
                virtInfo.UpdateIndex(index);
                _owner.OnElementIndexChanged(element, oldIndex, index);
            }
        }

        private void InvalidateRealizedIndicesHeldByLayout()
        {
            _firstRealizedElementIndexHeldByLayout = FirstRealizedElementIndexDefault;
            _lastRealizedElementIndexHeldByLayout = LastRealizedElementIndexDefault;
        }

        private struct PinnedElementInfo
        {
            public PinnedElementInfo(IControl element)
            {
                PinnedElement = element;
                VirtualizationInfo = ItemsRepeater.GetVirtualizationInfo(element);
            }

            public IControl PinnedElement { get; }
            public VirtualizationInfo VirtualizationInfo { get; }
        }
    }
}

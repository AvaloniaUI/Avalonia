using System;

namespace Avalonia.Controls.Repeaters
{
    internal enum ElementOwner
    {
        // All elements are originally owned by the view generator.
        ElementFactory,
        // Ownership is transferred to the layout when it calls GetElement.
        Layout,
        // Ownership is transferred to the pinned pool if the element is cleared (outside of
        // a 'remove' collection change of course).
        PinnedPool,
        // Ownership is transfered to the reset pool if the element is cleared by a reset and
        // the data source supports unique ids.
        UniqueIdResetPool,
        // Ownership is transfered to the animator if the element is cleared due to a
        // 'remove'-like collection change.
        Animator
    }

    internal class VirtualizationInfo
    {
        private int _pinCounter;
        private object _data;

        public Rect ArrangeBounds { get; set; }
        public bool AutoRecycleCandidate { get; set; }
        public int Index { get; private set; }
        public bool IsPinned => _pinCounter > 0;
        public bool IsHeldByLayout => Owner == ElementOwner.Layout;
        public bool IsRealized => IsHeldByLayout || Owner == ElementOwner.PinnedPool;
        public bool IsInUniqueIdResetPool => Owner == ElementOwner.UniqueIdResetPool;
        public bool KeepAlive { get; set; }
        public ElementOwner Owner { get; private set; } = ElementOwner.ElementFactory;
        public string UniqueId { get; private set; }

        public void MoveOwnershipToLayoutFromElementFactory(int index, string uniqueId)
        {
            //MUX_ASSERT(_owner == ElementOwner.ElementFactory);
            Owner = ElementOwner.Layout;
            Index = index;
            UniqueId = uniqueId;
        }

        public void MoveOwnershipToLayoutFromUniqueIdResetPool()
        {
            //MUX_ASSERT(_owner == ElementOwner.UniqueIdResetPool);
            Owner = ElementOwner.Layout;
        }

        public void MoveOwnershipToLayoutFromPinnedPool()
        {
            //MUX_ASSERT(_owner == ElementOwner.PinnedPool);
            //MUX_ASSERT(IsPinned());
            Owner = ElementOwner.Layout;
        }

        public void MoveOwnershipToElementFactory()
        {
            //MUX_ASSERT(_owner != ElementOwner.ElementFactory);
            Owner = ElementOwner.ElementFactory;
            _pinCounter = 0;
            Index = -1;
            UniqueId = string.Empty;
            ArrangeBounds = ItemsRepeater.InvalidRect;
        }

        public void MoveOwnershipToUniqueIdResetPoolFromLayout()
        {
            //MUX_ASSERT(_owner == ElementOwner.Layout);
            Owner = ElementOwner.UniqueIdResetPool;
            // Keep the pinCounter the same. If the container survives the reset
            // it can go on being pinned as if nothing happened.
        }

        public void MoveOwnershipToAnimator()
        {
            // During a unique id reset, some elements might get removed.
            // Their ownership will go from the UniqueIdResetPool to the Animator.
            // The common path though is for ownership to go from Layout to Animator.
            //MUX_ASSERT(_owner == ElementOwner.Layout || _owner == ElementOwner.UniqueIdResetPool);
            Owner = ElementOwner.Animator;
            Index = -1;
            _pinCounter = 0;
        }

        public void MoveOwnershipToPinnedPool()
        {
            //MUX_ASSERT(_owner == ElementOwner.Layout);
            Owner = ElementOwner.PinnedPool;
        }

        public int AddPin()
        {
            if (!IsRealized)
            {
                throw new InvalidOperationException("You can't pin an unrealized element.");
            }

            return ++_pinCounter;
        }

        public int RemovePin()
        {
            if (!IsRealized)
            {
                throw new InvalidOperationException("You can't unpin an unrealized element.");
            }

            if (!IsPinned)
            {
                throw new InvalidOperationException("UnpinElement was called more often than PinElement.");
            }

            return --_pinCounter;
        }

        public void UpdateIndex(int newIndex) => Index = newIndex;
    }
}

// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Collections;

namespace Avalonia.Controls
{

    internal class DataGridRowGroupInfo
    {
        public DataGridRowGroupInfo(
            DataGridCollectionViewGroup collectionViewGroup,
            bool isVisible,
            int level,
            int slot,
            int lastSubItemSlot)
        {
            CollectionViewGroup = collectionViewGroup;
            IsVisible = isVisible;
            Level = level;
            Slot = slot;
            LastSubItemSlot = lastSubItemSlot;
        }

        public DataGridCollectionViewGroup CollectionViewGroup
        {
            get;
            private set;
        }

        public int LastSubItemSlot
        {
            get;
            set;
        }

        public int Level
        {
            get;
            private set;
        }

        public int Slot
        {
            get;
            set;
        }

        public bool IsVisible
        {
            get;
            set;
        }
    }
}

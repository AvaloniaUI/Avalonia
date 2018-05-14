// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;

namespace Avalonia.Controls
{

    internal class DataGridRowGroupInfo
    {
        //TODO
        //RowGroups
        public DataGridRowGroupInfo(
            object collectionViewGroup,
            //CollectionViewGroup collectionViewGroup,
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

        //TODO
        //RowGroups
        public object CollectionViewGroup
        //public CollectionViewGroup CollectionViewGroup
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

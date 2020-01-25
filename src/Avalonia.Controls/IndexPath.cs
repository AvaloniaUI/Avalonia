// This source file is adapted from the WinUI project.
// (https://github.com/microsoft/microsoft-ui-xaml)
//
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Controls
{
    public sealed class IndexPath : IComparable<IndexPath>
    {
        private readonly List<int> _path = new List<int>();

        internal IndexPath(int index)
        {
            _path.Add(index);
        }

        internal IndexPath(int groupIndex, int itemIndex)
        {
            _path.Add(groupIndex);
            _path.Add(itemIndex);
        }

        internal IndexPath(IEnumerable<int> indices)
        {
            if (indices != null)
            {
                _path.AddRange(indices);
            }
        }

        public int GetSize() => _path.Count;
        public int GetAt(int index) => _path[index];

        public int CompareTo(IndexPath other)
        {
            var rhsPath = other;
            int compareResult = 0;
            int lhsCount = _path.Count;
            int rhsCount = rhsPath._path.Count;

            if (lhsCount == 0 || rhsCount == 0)
            {
                // one of the paths are empty, compare based on size
                compareResult = (lhsCount - rhsCount);
            }
            else
            {
                // both paths are non-empty, but can be of different size
                for (int i = 0; i < Math.Min(lhsCount, rhsCount); i++)
                {
                    if (_path[i] < rhsPath._path[i])
                    {
                        compareResult = -1;
                        break;
                    }
                    else if (_path[i] > rhsPath._path[i])
                    {
                        compareResult = 1;
                        break;
                    }
                }

                // if both match upto min(lhsCount, rhsCount), compare based on size
                compareResult = compareResult == 0 ? (lhsCount - rhsCount) : compareResult;
            }

            if (compareResult != 0)
            {
                compareResult = compareResult > 0 ? 1 : -1;
            }

            return compareResult;
        }

        public IndexPath CloneWithChildIndex(int childIndex)
        {
            var newPath = new List<int>(_path);
            newPath.Add(childIndex);
            return new IndexPath(newPath);
        }

        public override string ToString()
        {
            return "R." + string.Join(".", _path);
        }

        public static IndexPath CreateFrom(int index) => new IndexPath(index);

        public static IndexPath CreateFrom(int groupIndex, int itemIndex) => new IndexPath(groupIndex, itemIndex);

        public static IndexPath CreateFromIndices(IList<int> indices) => new IndexPath(indices);

    }
}

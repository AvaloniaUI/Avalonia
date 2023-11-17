using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace Avalonia.Controls.Utils
{
    internal class VirtualizingSnapPointsList : IReadOnlyList<double>
    {
        private const int ExtraCount = 2;
        private readonly RealizedStackElements _realizedElements;
        private readonly Orientation _orientation;
        private readonly Orientation _parentOrientation;
        private readonly SnapPointsAlignment _snapPointsAlignment;
        private readonly double _size;
        private readonly int _start = -1;
        private readonly int _end;

        public VirtualizingSnapPointsList(RealizedStackElements realizedElements, int count, Orientation orientation, Orientation parentOrientation, SnapPointsAlignment snapPointsAlignment, double size)
        {
            _realizedElements = realizedElements;
            _orientation = orientation;
            _parentOrientation = parentOrientation;
            _snapPointsAlignment = snapPointsAlignment;
            _size = size;
            if (parentOrientation == orientation)
            {
                _start = Math.Max(0, _realizedElements.FirstIndex - ExtraCount);
                _end = Math.Min(count - 1, _realizedElements.LastIndex + ExtraCount);
            }
        }

        public double this[int index]
        {
            get
            {
                if(index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                index += _start;

                double snapPoint = 0;
                var averageElementSize = _size;

                Control? container;
                switch (_orientation)
                {
                    case Orientation.Horizontal:
                        container = _realizedElements.GetElement(index);
                        if (container != null)
                        {
                            switch (_snapPointsAlignment)
                            {
                                case SnapPointsAlignment.Near:
                                    snapPoint = container.Bounds.Left;
                                    break;
                                case SnapPointsAlignment.Center:
                                    snapPoint = container.Bounds.Center.X;
                                    break;
                                case SnapPointsAlignment.Far:
                                    snapPoint = container.Bounds.Right;
                                    break;
                            }
                        }
                        else
                        {
                            var ind = index;
                            if (index > _realizedElements.LastIndex)
                            {
                                ind -= _realizedElements.LastIndex + 1;
                            }
                            snapPoint = ind * averageElementSize;
                            switch (_snapPointsAlignment)
                            {
                                case SnapPointsAlignment.Center:
                                    snapPoint += averageElementSize / 2;
                                    break;
                                case SnapPointsAlignment.Far:
                                    snapPoint += averageElementSize;
                                    break;
                            }
                            if (index > _realizedElements.LastIndex)
                            {
                                var lastElement = _realizedElements.GetElement(_realizedElements.LastIndex);
                                if (lastElement != null)
                                {
                                    snapPoint += lastElement.Bounds.Right;
                                }
                            }
                        }
                        break;
                    case Orientation.Vertical:
                        container = _realizedElements.GetElement(index);
                        if (container != null)
                        {
                            switch (_snapPointsAlignment)
                            {
                                case SnapPointsAlignment.Near:
                                    snapPoint = container.Bounds.Top;
                                    break;
                                case SnapPointsAlignment.Center:
                                    snapPoint = container.Bounds.Center.Y;
                                    break;
                                case SnapPointsAlignment.Far:
                                    snapPoint = container.Bounds.Bottom;
                                    break;
                            }
                        }
                        else
                        {
                            var ind = index;
                            if(index > _realizedElements.LastIndex)
                            {
                                ind -= _realizedElements.LastIndex + 1;
                            }
                            snapPoint = ind * averageElementSize;
                            switch (_snapPointsAlignment)
                            {
                                case SnapPointsAlignment.Center:
                                    snapPoint += averageElementSize / 2;
                                    break;
                                case SnapPointsAlignment.Far:
                                    snapPoint += averageElementSize;
                                    break;
                            }
                            if (index > _realizedElements.LastIndex)
                            {
                                var lastElement = _realizedElements.GetElement(_realizedElements.LastIndex);
                                if (lastElement != null)
                                {
                                    snapPoint += lastElement.Bounds.Bottom;
                                }
                            }
                        }
                        break;
                }

                return snapPoint;
            }
        }

        public int Count => _parentOrientation != _orientation ? 0 : _end - _start + 1;

        public IEnumerator<double> GetEnumerator()
        {
            for (var i = 0; i < Count; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

using System;
using Avalonia.Controls;
using Avalonia.Input.Raw;
using Avalonia.Platform;

namespace Avalonia
{
    internal class WindowResizeDragHelper
    {
        private IWindowImpl _window;
        private WindowEdge? _edge;
        private Point? _startPointerPoint;
        private Point? _originalWindowPosition;
        private Size? _originalWindowSize;
        private readonly Action<bool> _captureMouse;

        public double RenderScaling => _window.RenderScaling;

        public WindowResizeDragHelper(IWindowImpl window, Action<bool> captureMouse)
        {
            _window = window;
            _captureMouse = captureMouse;
        }

        public void StartDrag(WindowEdge edge, PixelPoint startDragPosition)
        {
            _captureMouse(true);
            _edge = edge;
            _startPointerPoint = startDragPosition.ToPoint(RenderScaling);


            _originalWindowPosition = _window.Position.ToPoint(RenderScaling);
            _originalWindowSize = _window.ClientSize;
        }

        public bool PreprocessInputEvent(ref RawInputEventArgs e)
        {
            if (_edge == null)
                return false;
            if (e is RawPointerEventArgs args)
            {
                if (args.Type == RawPointerEventType.LeftButtonUp)
                {
                    _edge = null;
                    _startPointerPoint = null;
                    _originalWindowPosition = null;
                    _originalWindowSize = null;
                    _captureMouse(false);
                }
                if (args.Type == RawPointerEventType.Move)
                {
                    MoveWindow(args.Position);
                    return true;
                }


                _edge = null;
            }

            return false;
        }


        private void MoveWindow(Point currentPositionInScreen)
        {
            Point diff = currentPositionInScreen - _startPointerPoint!.Value;

            var rc = new Rect(_originalWindowPosition!.Value, _originalWindowSize!.Value);

            if (_edge == WindowEdge.West || _edge == WindowEdge.NorthWest || _edge == WindowEdge.SouthWest)
            {
                rc = rc.WithX(rc.X + diff.X).WithWidth(rc.Width - diff.X);
                ;
            }
            if (_edge == WindowEdge.North || _edge == WindowEdge.NorthWest || _edge == WindowEdge.NorthEast)
            {
                rc = rc.WithY(rc.Y + diff.Y).WithHeight(rc.Height - diff.Y);
            }
            if (_edge == WindowEdge.East || _edge == WindowEdge.NorthEast || _edge == WindowEdge.SouthEast)
            {
                rc = rc.WithWidth(rc.Width + diff.X);
            }
            if (_edge == WindowEdge.South || _edge == WindowEdge.SouthWest || _edge == WindowEdge.SouthEast)
            {
                rc = rc.WithHeight(rc.Height + diff.Y);
            }

            PixelPoint rcPosition = PixelPoint.FromPoint(rc.Position, RenderScaling);

            if (_window.Position != rcPosition)
            {
                _window.Move(rcPosition);
            }
            if (_window.ClientSize != rc.Size)
            {
                _window.Resize(rc.Size);
            }
        }
    }
}

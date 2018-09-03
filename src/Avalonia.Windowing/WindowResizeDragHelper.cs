﻿using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;

namespace Avalonia
{
    internal class ManagedWindowResizeDragHelper
    {
        private readonly IWindowBaseImpl _window;
        private readonly Action<bool> _captureMouse;
        private readonly Action<Rect> _resize;
        private WindowEdge? _edge;
        private Point _prevPoint;
        private bool _isMove;

        public ManagedWindowResizeDragHelper(IWindowBaseImpl window, Action<bool> captureMouse, Action<Rect> resize = null)
        {
            _window = window;
            _captureMouse = captureMouse;
            _resize = resize;
        }

        public void BeginResizeDrag(WindowEdge edge, Point currentMousePosition)
        {
            _captureMouse(true);
            _prevPoint = currentMousePosition;
            _edge = edge;
        }

        public void BeginMoveDrag(Point currentMousePosition)
        {
            _captureMouse(true);
            _prevPoint = currentMousePosition;
            _edge = null;
            _isMove = true;
        }

        public bool PreprocessInputEvent(ref RawInputEventArgs e)
        {
            if (_edge == null && !_isMove)
                return false;
            if (e is RawMouseEventArgs args)
            {
                if (args.Type == RawMouseEventType.LeftButtonUp)
                {
                    _edge = null;
                    _isMove = false;
                    _captureMouse(false);
                }
                if (args.Type == RawMouseEventType.Move)
                {
                    MoveWindow(args.Position);
                    return true;
                }


                _edge = null;
            }

            return false;
        }

        private void MoveWindow(Point position)
        {
            var diff = position - _prevPoint;
            var rc = new Rect(_window.Position, _window.ClientSize);

            if (_edge == null && _isMove)
            {
                rc = rc.WithX(rc.X + diff.X).WithY(rc.Y + diff.Y);

                if (_resize != null)
                {
                    _resize(rc);
                }
                else
                {
                    _window.Position = rc.Position;
                }
            }
            else
            {

                var edge = _edge.Value;

                if (edge == WindowEdge.East || edge == WindowEdge.NorthEast || edge == WindowEdge.SouthEast)
                {
                    rc = rc.WithWidth(rc.Width + diff.X);
                    _prevPoint = _prevPoint.WithX(position.X);
                }
                if (edge == WindowEdge.West || edge == WindowEdge.NorthWest || edge == WindowEdge.SouthWest)
                    rc = rc.WithX(rc.X + diff.X).WithWidth(rc.Width - diff.X);
                if (edge == WindowEdge.South || edge == WindowEdge.SouthWest || edge == WindowEdge.SouthEast)
                {
                    rc = rc.WithHeight(rc.Height + diff.Y);
                    _prevPoint = _prevPoint.WithY(position.Y);
                }
                if (edge == WindowEdge.North || edge == WindowEdge.NorthWest || edge == WindowEdge.NorthEast)
                    rc = rc.WithY(rc.Y + diff.Y).WithHeight(rc.Height - diff.Y);

                if (_resize != null)
                    _resize(rc);
                else
                {
                    if (_window.Position != rc.Position)
                        _window.Position = rc.Position;
                    if (_window.ClientSize != rc.Size)
                        _window.Resize(rc.Size);
                }
            }
        }
    }
}
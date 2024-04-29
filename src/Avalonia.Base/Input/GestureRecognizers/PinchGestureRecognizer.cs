using Avalonia.Input.GestureRecognizers;

namespace Avalonia.Input
{
    public class PinchGestureRecognizer : GestureRecognizer
    {
        private float _initialDistance;
        private IPointer? _firstContact;
        private Point _firstPoint;
        private IPointer? _secondContact;
        private Point _secondPoint;
        private Point _origin;
        private double _previousAngle;

        protected override void PointerCaptureLost(IPointer pointer)
        {
            RemoveContact(pointer);
        }

        protected override void PointerMoved(PointerEventArgs e)
        {
            if (Target is Visual visual)
            {
                if (_firstContact == e.Pointer)
                {
                    _firstPoint = e.GetPosition(visual);
                }
                else if (_secondContact == e.Pointer)
                {
                    _secondPoint = e.GetPosition(visual);
                }
                else
                {
                    return;
                }

                if (_firstContact != null && _secondContact != null)
                {
                    var distance = GetDistance(_firstPoint, _secondPoint);

                    var scale = distance / _initialDistance;

                    var degree = GetAngleDegreeFromPoints(_firstPoint, _secondPoint);

                    var pinchEventArgs = new PinchEventArgs(scale, _origin, degree, _previousAngle - degree);
                    _previousAngle = degree;
                    Target?.RaiseEvent(pinchEventArgs);
                    e.Handled = pinchEventArgs.Handled;
                    e.PreventGestureRecognition();
                }
            }
        }

        protected override void PointerPressed(PointerPressedEventArgs e)
        {
            if (Target is Visual visual && (e.Pointer.Type == PointerType.Touch || e.Pointer.Type == PointerType.Pen))
            {
                if (_firstContact == null)
                {
                    _firstContact = e.Pointer;
                    _firstPoint = e.GetPosition(visual);

                    return;
                }
                else if (_secondContact == null && _firstContact != e.Pointer)
                {
                    _secondContact = e.Pointer;
                    _secondPoint = e.GetPosition(visual);
                }
                else
                {
                    return;
                }

                if (_firstContact != null && _secondContact != null)
                {
                    _initialDistance = GetDistance(_firstPoint, _secondPoint);

                    _origin = new Point((_firstPoint.X + _secondPoint.X) / 2.0f, (_firstPoint.Y + _secondPoint.Y) / 2.0f);

                    _previousAngle = GetAngleDegreeFromPoints(_firstPoint, _secondPoint);

                    Capture(_firstContact);
                    Capture(_secondContact);
                    e.PreventGestureRecognition();
                }
            }
        }

        protected override void PointerReleased(PointerReleasedEventArgs e)
        {
            if(RemoveContact(e.Pointer))
            {
                e.PreventGestureRecognition();
            }
        }

        private bool RemoveContact(IPointer pointer)
        {
            if (_firstContact == pointer || _secondContact == pointer)
            {
                if (_secondContact == pointer)
                {
                    _secondContact = null;
                }

                if (_firstContact == pointer)
                {
                    _firstContact = _secondContact;

                    _secondContact = null;
                }

                Target?.RaiseEvent(new PinchEndedEventArgs());
                return true;
            }
            return false;
        }

        private static float GetDistance(Point a, Point b)
        {
            var length = b - a;
            return (float)new Vector(length.X, length.Y).Length;
        }

        private static double GetAngleDegreeFromPoints(Point a, Point b)
        {
            // https://stackoverflow.com/a/15994225/20894223

            var deltaX = a.X - b.X;
            var deltaY = -(a.Y - b.Y);                           // I reverse the sign, because on the screen the Y axes
                                                                 // are reversed with respect to the Cartesian plane.
            var rad = System.Math.Atan2(deltaX, deltaY);         // radians from -π to +π
            var degree = ((rad * (180 / System.Math.PI))) + 180; // Atan2 returns a radian value between -π to +π, in degrees -180 to +180.
                                                                 // To get the angle between 0 and 360 degrees you need to add 180 degrees.
            return degree;
        }
    }
}

// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using Avalonia.Input;
using System.Diagnostics;

namespace Avalonia.Controls.Primitives
{
    internal static class CalendarExtensions
    {

        private static Dictionary<IAvaloniaObject, Dictionary<AvaloniaProperty, bool>> _suspendedHandlers = new Dictionary<IAvaloniaObject, Dictionary<AvaloniaProperty, bool>>();

        public static bool IsHandlerSuspended(this IAvaloniaObject obj, AvaloniaProperty dependencyProperty)
        {
            if (_suspendedHandlers.ContainsKey(obj))
            {
                return _suspendedHandlers[obj].ContainsKey(dependencyProperty);
            }
            else
            {
                return false;
            }
        }
        private static void SuspendHandler(this IAvaloniaObject obj, AvaloniaProperty dependencyProperty, bool suspend)
        {
            if (_suspendedHandlers.ContainsKey(obj))
            {
                Dictionary<AvaloniaProperty, bool> suspensions = _suspendedHandlers[obj];

                if (suspend)
                {
                    Debug.Assert(!suspensions.ContainsKey(dependencyProperty), "Suspensions should not contain the property!");

                    // true = dummy value
                    suspensions[dependencyProperty] = true;
                }
                else
                {
                    Debug.Assert(suspensions.ContainsKey(dependencyProperty), "Suspensions should contain the property!");
                    suspensions.Remove(dependencyProperty);
                    if (suspensions.Count == 0)
                    {
                        _suspendedHandlers.Remove(obj);
                    }
                }
            }
            else
            {
                Debug.Assert(suspend, "suspend should be true!");
                _suspendedHandlers[obj] = new Dictionary<AvaloniaProperty, bool>();
                _suspendedHandlers[obj][dependencyProperty] = true;
            }
        }
        public static void SetValueNoCallback<T>(this IAvaloniaObject obj, AvaloniaProperty<T> property, T value)
        {
            obj.SuspendHandler(property, true);
            try
            {
                obj.SetValue(property, value);
            }
            finally
            {
                obj.SuspendHandler(property, false);
            }
        }
        
        public static void GetMetaKeyState(InputModifiers modifiers, out bool ctrl, out bool shift)
        {
            ctrl = (modifiers & InputModifiers.Control) == InputModifiers.Control;
            shift = (modifiers & InputModifiers.Shift) == InputModifiers.Shift;
        }
    }
}

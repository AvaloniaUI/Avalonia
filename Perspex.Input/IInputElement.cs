// -----------------------------------------------------------------------
// <copyright file="IInputElement.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;
    using Perspex.Interactivity;

    public interface IInputElement : IInteractive
    {
        event EventHandler<RoutedEventArgs> GotFocus;

        event EventHandler<RoutedEventArgs> LostFocus;

        event EventHandler<KeyEventArgs> KeyDown;

        event EventHandler<PointerEventArgs> PointerEnter;

        event EventHandler<PointerEventArgs> PointerLeave;

        event EventHandler<PointerEventArgs> PointerPressed;

        event EventHandler<PointerEventArgs> PointerReleased;

        bool Focusable { get; }

        bool IsEnabled { get; }

        bool IsEnabledCore { get; }

        bool IsFocused { get; }

        bool IsPointerOver { get; }

        void Focus();

        IInputElement InputHitTest(Point p);
    }
}

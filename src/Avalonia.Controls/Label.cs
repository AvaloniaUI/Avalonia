﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Label control. Focuses <see cref="Target"/> on pointer click or access key press (Alt + accessKey)
    /// </summary>
    public class Label : ContentControl
    {
        /// <summary>
        /// Defines the <see cref="Target"/> Direct property
        /// </summary>
        public static readonly DirectProperty<Label, IInputElement?> TargetProperty =
            AvaloniaProperty.RegisterDirect<Label, IInputElement?>(nameof(Target), lbl => lbl.Target, (lbl, inp) => lbl.Target = inp);

        /// <summary>
        /// Label focus target storage field
        /// </summary>
        private IInputElement? _target;

        /// <summary>
        /// Label focus Target
        /// </summary>
        [ResolveByName]
        public IInputElement? Target
        {
            get => _target;
            set => SetAndRaise(TargetProperty, ref _target, value);
        }

        static Label()
        {
            AccessKeyHandler.AccessKeyPressedEvent.AddClassHandler<Label>((lbl, args) => lbl.LabelActivated(args));
            // IsTabStopProperty.OverrideDefaultValue<Label>(false)
            FocusableProperty.OverrideDefaultValue<Label>(false);
        }

        /// <summary>
        /// Initializes instance of <see cref="Label"/> control
        /// </summary>
        public Label()
        {
        }

        /// <summary>
        /// Method which focuses <see cref="Target"/> input element
        /// </summary>
        private void LabelActivated(RoutedEventArgs args)
        {
            Target?.Focus();
            args.Handled = Target != null;
        }

        /// <summary>
        /// Handler of <see cref="IInputElement.PointerPressed"/> event
        /// </summary>
        /// <param name="e">Event Arguments</param>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                LabelActivated(e);
            }
            base.OnPointerPressed(e);
        }
    }
}

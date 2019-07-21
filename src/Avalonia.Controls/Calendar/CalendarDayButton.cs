// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Globalization;
using Avalonia.Input;

namespace Avalonia.Controls.Primitives
{
    public sealed class CalendarDayButton : Button
    {
        /// <summary>
        /// Default content for the CalendarDayButton.
        /// </summary>
        private const int DefaultContent = 1;

        private bool _isCurrent;
        private bool _ignoringMouseOverState;
        private bool _isBlackout;
        private bool _isToday;
        private bool _isInactive;
        private bool _isSelected;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:Avalonia.Controls.Primitives.CalendarDayButton" />
        /// class.
        /// </summary>
        public CalendarDayButton()
            : base()
        {
            //Focusable = false;
            Content = DefaultContent.ToString(CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Gets or sets the Calendar associated with this button.
        /// </summary>
        internal Calendar Owner { get; set; }
        internal int Index { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the button is the focused
        /// element on the Calendar control.
        /// </summary>
        internal bool IsCurrent
        {
            get { return _isCurrent; }
            set
            {
                if (_isCurrent != value)
                {
                    _isCurrent = value;
                    SetPseudoClasses();
                }
            }
        }

        /// <summary>
        /// Ensure the button is not in the MouseOver state.
        /// </summary>
        /// <remarks>
        /// If a button is in the MouseOver state when a Popup is closed (as is
        /// the case when you select a date in the DatePicker control), it will
        /// continue to think it's in the mouse over state even when the Popup
        /// opens again and it's not.  This method is used to forcibly clear the
        /// state by changing the CommonStates state group.
        /// </remarks>
        internal void IgnoreMouseOverState()
        {
            // TODO: Investigate whether this needs to be done by changing the
            // state everytime we change any state, or if it can be done once
            // to properly reset the control.

            _ignoringMouseOverState = false;

            // If the button thinks it's in the MouseOver state (which can
            // happen when a Popup is closed before the button can change state)
            // we will override the state so it shows up as normal.
            if (IsPointerOver)
            {
                _ignoringMouseOverState = true;
                SetPseudoClasses();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this is a blackout date.
        /// </summary>
        internal bool IsBlackout
        {
            get { return _isBlackout; }
            set
            {
                if (_isBlackout != value)
                {
                    _isBlackout = value;
                    SetPseudoClasses();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this button represents
        /// today.
        /// </summary>
        internal bool IsToday
        {
            get { return _isToday; }
            set
            {
                if (_isToday != value)
                {
                    _isToday = value;
                    SetPseudoClasses();
                }
            }
        }
        /// <summary>
        /// Gets or sets a value indicating whether the button is inactive.
        /// </summary>
        internal bool IsInactive
        {
            get { return _isInactive; }
            set
            {
                if (_isInactive != value)
                {
                    _isInactive = value;
                    SetPseudoClasses();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the button is selected.
        /// </summary>
        internal bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    SetPseudoClasses();
                }
            }
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);
            SetPseudoClasses();
        }
        private void SetPseudoClasses()
        {
            if (_ignoringMouseOverState)
            {
                PseudoClasses.Set(":pressed", IsPressed);
                PseudoClasses.Set(":disabled", !IsEnabled);
            }

            PseudoClasses.Set(":selected", IsSelected);
            PseudoClasses.Set(":inactive", IsInactive);
            PseudoClasses.Set(":today", IsToday);
            PseudoClasses.Set(":blackout", IsBlackout);
            PseudoClasses.Set(":dayfocused", IsCurrent && IsEnabled);
        }

        /// <summary>
        /// Occurs when the left mouse button is pressed (or when the tip of the
        /// stylus touches the tablet PC) while the mouse pointer is over a
        /// UIElement.
        /// </summary>
        public event EventHandler<PointerPressedEventArgs> CalendarDayButtonMouseDown;

        /// <summary>
        /// Occurs when the left mouse button is released (or the tip of the
        /// stylus is removed from the tablet PC) while the mouse (or the
        /// stylus) is over a UIElement (or while a UIElement holds mouse
        /// capture).
        /// </summary>
        public event EventHandler<PointerReleasedEventArgs> CalendarDayButtonMouseUp;

        /// <summary>
        /// Provides class handling for the MouseLeftButtonDown event that
        /// occurs when the left mouse button is pressed while the mouse pointer
        /// is over this control.
        /// </summary>
        /// <param name="e">The event data. </param>
        /// <exception cref="System.ArgumentNullException">
        /// e is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <remarks>
        /// This method marks the MouseLeftButtonDown event as handled by
        /// setting the MouseButtonEventArgs.Handled property of the event data
        /// to true when the button is enabled and its ClickMode is not set to
        /// Hover.  Since this method marks the MouseLeftButtonDown event as
        /// handled in some situations, you should use the Click event instead
        /// to detect a button click.
        /// </remarks>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.MouseButton == MouseButton.Left)
                CalendarDayButtonMouseDown?.Invoke(this, e);
        }

        /// <summary>
        /// Provides handling for the MouseLeftButtonUp event that occurs when
        /// the left mouse button is released while the mouse pointer is over
        /// this control. 
        /// </summary>
        /// <param name="e">The event data.</param>
        /// <exception cref="System.ArgumentNullException">
        /// e is a null reference (Nothing in Visual Basic).
        /// </exception>
        /// <remarks>
        /// This method marks the MouseLeftButtonUp event as handled by setting
        /// the MouseButtonEventArgs.Handled property of the event data to true
        /// when the button is enabled and its ClickMode is not set to Hover.
        /// Since this method marks the MouseLeftButtonUp event as handled in
        /// some situations, you should use the Click event instead to detect a
        /// button click.
        /// </remarks>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (e.MouseButton == MouseButton.Left)
                CalendarDayButtonMouseUp?.Invoke(this, e);
        }
    }
}

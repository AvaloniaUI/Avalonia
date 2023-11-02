// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see https://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Controls.Metadata;
using Avalonia.Input;
using System;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents a button on a
    /// <see cref="T:Avalonia.Controls.Calendar" />.
    /// </summary>
    [PseudoClasses(":selected", ":inactive", ":btnfocused")]
    public sealed class CalendarButton : Button
    {
        /// <summary>
        /// A value indicating whether the button is focused.
        /// </summary>
        private bool _isCalendarButtonFocused;

        /// <summary>
        /// A value indicating whether the button is inactive.
        /// </summary>
        private bool _isInactive;

        /// <summary>
        /// A value indicating whether the button is selected.
        /// </summary>
        private bool _isSelected;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:Avalonia.Controls.Primitives.CalendarButton" />
        /// class.
        /// </summary>
        public CalendarButton()
            : base()
        {
            SetCurrentValue(ContentProperty, DateTimeHelper.GetCurrentDateFormat().AbbreviatedMonthNames[0]);
        }

        /// <summary>
        /// Gets or sets the Calendar associated with this button.
        /// </summary>
        internal Calendar? Owner { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the button is focused.
        /// </summary>
        internal bool IsCalendarButtonFocused
        {
            get => _isCalendarButtonFocused;
            set
            {
                if (_isCalendarButtonFocused != value)
                {
                    _isCalendarButtonFocused = value;
                    SetPseudoClasses();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the button is inactive.
        /// </summary>
        internal bool IsInactive
        {
            get => _isInactive;
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
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    SetPseudoClasses();
                }
            }
        }

        /// <summary>
        /// Builds the visual tree for the
        /// <see cref="T:System.Windows.Controls.Primitives.CalendarButton" />
        /// when a new template is applied.
        /// </summary>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            SetPseudoClasses();
        }
        
        /// <summary>
        /// Sets PseudoClasses based on current state.
        /// </summary>
        private void SetPseudoClasses()
        {
            PseudoClasses.Set(":selected", IsSelected);
            PseudoClasses.Set(":inactive", IsInactive);
            PseudoClasses.Set(":btnfocused", IsCalendarButtonFocused && IsEnabled);
        }

        /// <summary>
        /// Occurs when the left mouse button is pressed (or when the tip of the
        /// stylus touches the tablet PC) while the mouse pointer is over a
        /// UIElement.
        /// </summary>
        public event EventHandler<PointerPressedEventArgs>? CalendarLeftMouseButtonDown;

        /// <summary>
        /// Occurs when the left mouse button is released (or the tip of the
        /// stylus is removed from the tablet PC) while the mouse (or the
        /// stylus) is over a UIElement (or while a UIElement holds mouse
        /// capture).
        /// </summary>
        public event EventHandler<PointerReleasedEventArgs>? CalendarLeftMouseButtonUp;

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

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                CalendarLeftMouseButtonDown?.Invoke(this, e);
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
            if (e.InitialPressMouseButton == MouseButton.Left)
                CalendarLeftMouseButtonUp?.Invoke(this, e);
        }
    }
}

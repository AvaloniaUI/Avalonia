// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Manages focus for the application.
    /// </summary>
    public class FocusManager : IFocusManager
    {
        private readonly InputElement _root;
        private IInputElement _logicalFocus;
        private bool _hasEffectiveFocus;

        /// <summary>
        /// Initializes a new instance of the <see cref="FocusManager"/> class.
        /// </summary>
        public FocusManager(InputElement root)
        {
            _root = root;
            root.AddHandler(InputElement.PointerPressedEvent,
                new EventHandler<RoutedEventArgs>(OnPreviewPointerPressed),
                RoutingStrategies.Tunnel);
        }

        /// <summary>
        /// Gets the currently focused <see cref="IInputElement"/>.
        /// </summary>
        public IInputElement FocusedElement => _hasEffectiveFocus ? _logicalFocus : null;

        /// <summary>
        /// Is triggered when FocusedElement is changed
        /// </summary>
        public event EventHandler FocusedElementChanged;


        void UpdateFocus(Action cb, NavigationMethod method = NavigationMethod.Unspecified,
            InputModifiers modifiers = default)
        {
            var lastFocus = FocusedElement;
            cb();
            
            // For now reset the focus if control is detached from the root
            // We can make the focus 
            if (_logicalFocus != null && !_root.IsVisualAncestorOf(_logicalFocus))
                _logicalFocus = null;
            
            if (lastFocus != FocusedElement)
            {
                lastFocus?.RaiseEvent(new RoutedEventArgs {RoutedEvent = InputElement.LostFocusEvent});
                FocusedElement?.RaiseEvent(new GotFocusEventArgs
                {
                    RoutedEvent = InputElement.GotFocusEvent, NavigationMethod = method, InputModifiers = modifiers,
                });
            }

            FocusedElementChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetHasEffectiveFocus(bool hasEffectiveFocus)
        {
            UpdateFocus(() => _hasEffectiveFocus = hasEffectiveFocus);
        }

        /// <summary>
        /// Focuses a control.
        /// </summary>
        /// <param name="control">The control to focus.</param>
        /// <param name="method">The method by which focus was changed.</param>
        /// <param name="modifiers">Any input modifiers active at the time of focus.</param>
        public bool Focus(
            IInputElement control, 
            NavigationMethod method = NavigationMethod.Unspecified,
            InputModifiers modifiers = InputModifiers.None)
        {
            if (control != null && !_root.IsVisualAncestorOf(control))
                throw new InvalidOperationException("Visual to focus isn't a child of the controlled focus root");
            UpdateFocus(() =>
            {
                _logicalFocus = control;
                if (method == NavigationMethod.Pointer)
                {
                    // For now we assume that we have effective focus if there was a pointer event
                    _hasEffectiveFocus = true;
                }
            }, method, modifiers);
            return FocusedElement == control;
        }

        /// <summary>
        /// Checks if the specified element can be focused.
        /// </summary>
        /// <param name="e">The element.</param>
        /// <returns>True if the element can be focused.</returns>
        private static bool CanFocus(IInputElement e) => e.Focusable && e.IsEnabledCore && e.IsVisible;

        /// <summary>
        /// Global handler for pointer pressed events.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void OnPreviewPointerPressed(object sender, RoutedEventArgs e)
        {
            var ev = (PointerPressedEventArgs)e;

            if (ev.MouseButton == MouseButton.Left)
            {
                var element = (ev.Device?.Captured as IInputElement) ?? (e.Source as IInputElement);

                if (element == null || !CanFocus(element))
                {
                    element = element.GetSelfAndVisualAncestors()
                        .OfType<IInputElement>()
                        .FirstOrDefault(CanFocus);
                }

                if (element != null)
                {
                    Focus(element, NavigationMethod.Pointer, ev.InputModifiers);
                }
            }
        }
    }
}

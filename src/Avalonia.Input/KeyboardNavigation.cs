// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Input
{
    /// <summary>
    /// Defines attached properties that control keyboard navigation behaviour for a container.
    /// </summary>
    public static class KeyboardNavigation
    {
        /// <summary>
        /// Defines the DirectionalNavigation attached property.
        /// </summary>
        /// <remarks>
        /// The DirectionalNavigation attached property defines how pressing arrow keys causes
        /// focus to be navigated between the children of the container.
        /// </remarks>
        public static readonly AttachedProperty<KeyboardNavigationMode> DirectionalNavigationProperty =
            AvaloniaProperty.RegisterAttached<InputElement, KeyboardNavigationMode>(
                "DirectionalNavigation",
                typeof(KeyboardNavigation),
                KeyboardNavigationMode.None);

        /// <summary>
        /// Defines the TabNavigation attached property.
        /// </summary>
        /// <remarks>
        /// The TabNavigation attached property defines how pressing the Tab key causes focus to
        /// be navigated between the children of the container.
        /// </remarks>
        public static readonly AttachedProperty<KeyboardNavigationMode> TabNavigationProperty =
            AvaloniaProperty.RegisterAttached<InputElement, KeyboardNavigationMode>(
                "TabNavigation",
                typeof(KeyboardNavigation));

        /// <summary>
        /// Defines the TabOnceActiveElement attached property.
        /// </summary>
        /// <remarks>
        /// When focus enters a container which has its <see cref="TabNavigationProperty"/>
        /// attached property set to <see cref="KeyboardNavigationMode.Once"/>, this property
        /// defines to which child the focus should move.
        /// </remarks>
        public static readonly AttachedProperty<IInputElement> TabOnceActiveElementProperty =
            AvaloniaProperty.RegisterAttached<InputElement, IInputElement>(
                "TabOnceActiveElement",
                typeof(KeyboardNavigation));

        /// <summary>
        /// Gets the <see cref="DirectionalNavigationProperty"/> for a container.
        /// </summary>
        /// <param name="element">The container.</param>
        /// <returns>The <see cref="KeyboardNavigationMode"/> for the container.</returns>
        public static KeyboardNavigationMode GetDirectionalNavigation(InputElement element)
        {
            return element.GetValue(DirectionalNavigationProperty);
        }

        /// <summary>
        /// Sets the <see cref="DirectionalNavigationProperty"/> for a container.
        /// </summary>
        /// <param name="element">The container.</param>
        /// <param name="value">The <see cref="KeyboardNavigationMode"/> for the container.</param>
        public static void SetDirectionalNavigation(InputElement element, KeyboardNavigationMode value)
        {
            element.SetValue(DirectionalNavigationProperty, value);
        }

        /// <summary>
        /// Gets the <see cref="TabNavigationProperty"/> for a container.
        /// </summary>
        /// <param name="element">The container.</param>
        /// <returns>The <see cref="KeyboardNavigationMode"/> for the container.</returns>
        public static KeyboardNavigationMode GetTabNavigation(InputElement element)
        {
            return element.GetValue(TabNavigationProperty);
        }

        /// <summary>
        /// Sets the <see cref="TabNavigationProperty"/> for a container.
        /// </summary>
        /// <param name="element">The container.</param>
        /// <param name="value">The <see cref="KeyboardNavigationMode"/> for the container.</param>
        public static void SetTabNavigation(InputElement element, KeyboardNavigationMode value)
        {
            element.SetValue(TabNavigationProperty, value);
        }

        /// <summary>
        /// Gets the <see cref="TabOnceActiveElementProperty"/> for a container.
        /// </summary>
        /// <param name="element">The container.</param>
        /// <returns>The active element for the container.</returns>
        public static IInputElement GetTabOnceActiveElement(InputElement element)
        {
            return element.GetValue(TabOnceActiveElementProperty);
        }

        /// <summary>
        /// Sets the <see cref="TabOnceActiveElementProperty"/> for a container.
        /// </summary>
        /// <param name="element">The container.</param>
        /// <param name="value">The active element for the container.</param>
        public static void SetTabOnceActiveElement(InputElement element, IInputElement value)
        {
            element.SetValue(TabOnceActiveElementProperty, value);
        }
    }
}

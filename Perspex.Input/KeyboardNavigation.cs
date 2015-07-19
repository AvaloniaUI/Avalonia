// -----------------------------------------------------------------------
// <copyright file="KeyboardNavigation.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    /// <summary>
    /// Defines attached properties that control keyboard navigation behaviour for a container.
    /// </summary>
    public static class KeyboardNavigation
    {
        /// <summary>
        /// Defines the TabNavigation attached property.
        /// </summary>
        /// <remarks>
        /// The TabNavigation attached property defines how pressing the Tab key causes focus to
        /// be navigated between the children of the container.
        /// </remarks>
        public static readonly PerspexProperty<KeyboardNavigationMode> TabNavigationProperty =
            PerspexProperty.RegisterAttached<InputElement, KeyboardNavigationMode>("TabNavigation", typeof(KeyboardNavigation));

        /// <summary>
        /// Defines the TabOnceActiveElement attached property.
        /// </summary>
        /// <remarks>
        /// When focus enters a container which has its <see cref="TabNavigationProperty"/>
        /// attached property set to <see cref="KeyboardNavigationMode.Once"/>, this property
        /// defines to which child the focus should move.
        /// </remarks>
        public static readonly PerspexProperty<IInputElement> TabOnceActiveElementProperty =
            PerspexProperty.RegisterAttached<InputElement, IInputElement>("TabOnceActiveElement", typeof(KeyboardNavigation));

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

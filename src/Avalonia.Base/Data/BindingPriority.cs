using System;

namespace Avalonia.Data
{
    /// <summary>
    /// The priority of a binding.
    /// </summary>
    public enum BindingPriority
    {
        /// <summary>
        /// A value that comes from an animation.
        /// </summary>
        Animation = -1,

        /// <summary>
        /// A local value.
        /// </summary>
        LocalValue = 0,

        /// <summary>
        /// A triggered style value or binding.
        /// </summary>
        /// <remarks>
        /// A style trigger is a selector such as .class which overrides a
        /// <see cref="TemplatedParent"/> binding. In this way, a basic control can have
        /// for example a Background from the templated parent which changes when the
        /// control has the :pointerover class.
        /// </remarks>
        StyleTrigger,

        /// <summary>
        /// A binding to a property on the templated parent.
        /// </summary>
        [Obsolete]
        TemplatedParent,

        /// <summary>
        /// A style value or binding.
        /// </summary>
        Style,

        /// <summary>
        /// A triggered style value or binding within a control theme.
        /// </summary>
        ControlThemeTrigger,

        /// <summary>
        /// A control theme value or binding.
        /// </summary>
        ControlTheme,

        /// <summary>
        /// The value is inherited from an ancestor element.
        /// </summary>
        Inherited,

        /// <summary>
        /// The binding is uninitialized.
        /// </summary>
        Unset = int.MaxValue,
    }
}

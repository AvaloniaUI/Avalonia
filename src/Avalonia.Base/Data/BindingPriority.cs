using System;
using System.ComponentModel;

namespace Avalonia.Data
{
    /// <summary>
    /// The priority of a value or binding.
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
        /// A triggered style value.
        /// </summary>
        /// <remarks>
        /// A style trigger is a selector such as .class which overrides a
        /// <see cref="Template"/> value. In this way, a control can have, e.g. a Background from
        /// the template which changes when the control has the :pointerover class.
        /// </remarks>
        StyleTrigger,

        /// <summary>
        /// A value from the control's template.
        /// </summary>
        Template,

        /// <summary>
        /// A style value.
        /// </summary>
        Style,
        
        /// <summary>
        /// The value is inherited from an ancestor element.
        /// </summary>
        Inherited,

        /// <summary>
        /// The value is uninitialized.
        /// </summary>
        Unset = int.MaxValue,

        [Obsolete("Use Template priority"), EditorBrowsable(EditorBrowsableState.Never)]
        TemplatedParent = Template,
    }
}

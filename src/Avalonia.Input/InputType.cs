using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Input
{
    /// <summary>
    /// Input type enumeration
    /// </summary>
    public enum InputType
    {
        /// <summary>
        /// Do not use input
        /// </summary>
        None,

        /// <summary>
        /// User full text input
        /// </summary>
        Text,

        /// <summary>
        /// Use numeric text input
        /// </summary>
        Numeric,

        /// <summary>
        /// Use phone input
        /// </summary>
        Phone
    }
}

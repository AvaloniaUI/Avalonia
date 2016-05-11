// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Logging
{
    /// <summary>
    /// Specifies the area in which a log event occurred.
    /// </summary>
    public static class LogArea
    {
        /// <summary>
        /// The log event comes from the property system.
        /// </summary>
        public const string Property = "Property";

        /// <summary>
        /// The log event comes from the binding system.
        /// </summary>
        public const string Binding = "Binding";

        /// <summary>
        /// The log event comes from the visual system.
        /// </summary>
        public const string Visual = "Visual";

        /// <summary>
        /// The log event comes from the layout system.
        /// </summary>
        public const string Layout = "Layout";

        /// <summary>
        /// The log event comes from the control system.
        /// </summary>
        public const string Control = "Control";
    }
}

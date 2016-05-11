// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Data
{
    /// <summary>
    /// Defines possible binding modes.
    /// </summary>
    public enum BindingMode
    {
        /// <summary>
        /// Uses the default binding mode specified for the property.
        /// </summary>
        Default,

        /// <summary>
        /// Binds one way from source to target.
        /// </summary>
        OneWay,

        /// <summary>
        /// Binds two-way with the initial value coming from the target.
        /// </summary>
        TwoWay,

        /// <summary>
        /// Updates the target when the application starts or when the data context changes.
        /// </summary>
        OneTime,

        /// <summary>
        /// Binds one way from target to source.
        /// </summary>
        OneWayToSource,
    }
}

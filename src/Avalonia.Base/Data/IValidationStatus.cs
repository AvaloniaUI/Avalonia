// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Data
{
    /// <summary>
    /// Contains information on if the current object passed validation.
    /// Subclasses of this class contain additional information depending on the method of validation checking. 
    /// </summary>
    public interface IValidationStatus
    {
        /// <summary>
        /// True when the data passes validation; otherwise, false.
        /// </summary>
        bool IsValid { get; }
    }
}

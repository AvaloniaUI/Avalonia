// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Data
{
    /// <summary>
    /// Contains information on if the current object passed validation.
    /// Subclasses of this class contain additional information depending on the method of validation checking. 
    /// </summary>
    public abstract class ValidationStatus
    {
        /// <summary>
        /// True when the data passes validation; otherwise, false.
        /// </summary>
        public abstract bool IsValid { get; }

        /// <summary>
        /// Checks if this validation status came from a currently enabled method of validation checking.
        /// </summary>
        /// <param name="enabledMethods">The enabled methods of validation checking.</param>
        /// <returns>True if enabled; otherwise, false.</returns>
        public abstract bool Match(ValidationMethods enabledMethods);
    }
}

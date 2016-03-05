// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex
{
    /// <summary>
    /// Exception signifying an internal logic error in Perspex.
    /// </summary>
    public class PerspexInternalException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexInternalException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public PerspexInternalException(string message)
            : base(message)
        {
        }
    }
}

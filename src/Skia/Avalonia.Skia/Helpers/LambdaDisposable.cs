// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Skia.Helpers
{
    /// <summary>
    /// Executes a callback when dispose is called.
    /// </summary>
    internal class LambdaDisposable : IDisposable
    {
        private readonly Action _action;

        /// <summary>
        /// Create new lambda disposable.
        /// </summary>
        /// <param name="action">Callback to call when disposing.</param>
        public LambdaDisposable(Action action)
        {
            _action = action;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _action();
        }
    }
}
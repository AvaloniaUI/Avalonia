// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Controls
{
    /// <summary>
    /// Contains the arguments for the <see cref="IApplicationLifecycle.Startup"/> event.
    /// </summary>
    public class StartupEventArgs : EventArgs
    {
        private string[] _args;

        /// <summary>
        /// Gets command line arguments that were passed to the application.
        /// </summary>
        public IReadOnlyList<string> Args => _args ?? (_args = GetArgs());

        private static string[] GetArgs()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();

                return args.Length > 1 ? args.Skip(1).ToArray() : new string[0];
            }
            catch (NotSupportedException)
            {
                return new string[0];
            }
        }
    }
}

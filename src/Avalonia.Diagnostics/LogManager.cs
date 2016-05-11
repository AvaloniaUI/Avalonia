// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Layout;
using Splat;

namespace Avalonia.Diagnostics
{
    public class LogManager : ILogManager
    {
        private static LogManager s_instance;

        public static LogManager Instance => s_instance ?? (s_instance = new LogManager());

        public ILogger Logger
        {
            get;
            set;
        }

        public bool LogPropertyMessages
        {
            get;
            set;
        }

        public bool LogLayoutMessages
        {
            get;
            set;
        }

        public static void Enable(ILogger logger)
        {
            Instance.Logger = logger;
            Locator.CurrentMutable.Register(() => Instance, typeof(ILogManager));
        }

        public IFullLogger GetLogger(Type type)
        {
            if ((type == typeof(AvaloniaObject) && LogPropertyMessages) ||
                (type == typeof(Layoutable) && LogLayoutMessages))
            {
                return new WrappingFullLogger(Logger, type);
            }
            else
            {
                return new WrappingFullLogger(new NullLogger(), type);
            }
        }
    }
}

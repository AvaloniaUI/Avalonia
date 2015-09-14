// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Layout;
using Splat;

namespace Perspex.Diagnostics
{
    public class LogManager : ILogManager
    {
        private static LogManager s_instance;

        public static LogManager Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new LogManager();
                }

                return s_instance;
            }
        }

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
            if ((type == typeof(PerspexObject) && LogPropertyMessages) ||
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

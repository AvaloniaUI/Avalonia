// -----------------------------------------------------------------------
// <copyright file="LogManager.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics
{
    using System;
    using Perspex.Layout;
    using Splat;

    public class LogManager : ILogManager
    {
        private static LogManager instance;

        public static LogManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LogManager();
                }

                return instance;
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
            if ((type == typeof(PerspexObject) && this.LogPropertyMessages) ||
                (type == typeof(Layoutable) && this.LogLayoutMessages))
            {
                return new WrappingFullLogger(this.Logger, type);
            }
            else
            {
                return new WrappingFullLogger(new NullLogger(), type);
            }
        }
    }
}

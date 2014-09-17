// -----------------------------------------------------------------------
// <copyright file="Dispatcher.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Threading
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Security;
    using System.Threading;
    using Perspex.Platform;
    using Splat;

    public enum DispatcherPriority
    {
        Invalid = -1,
        Inactive = 0,
        SystemIdle = 1,
        ApplicationIdle = 2,
        ContextIdle = 3,
        Background = 4,
        Input = 5,
        Loaded = 6,
        Render = 7,
        DataBind = 8,
        Normal = 9,
        Send = 10,
    }

    [Flags]
    internal enum Flags
    {
        ShutdownStarted = 1,
        Shutdown = 2,
        Disabled = 4
    }

    public abstract class Dispatcher
    {
        private static DispatcherFrame mainExecutionFrame = new DispatcherFrame();

        public static Dispatcher CurrentDispatcher
        {
            get { return Locator.Current.GetService<IPlatformThreadingInterface>().GetThreadDispatcher(); }
        }

        public abstract bool HasShutdownFinished
        {
            get;
        }

        public abstract DispatcherFrame CurrentFrame
        {
            get;
            set;
        }

        public static void PushFrame(DispatcherFrame frame)
        {
            if (frame == null)
            {
                throw new ArgumentNullException("frame");
            }

            Dispatcher dis = CurrentDispatcher;

            if (dis.HasShutdownFinished)
            {
                throw new InvalidOperationException("The Dispatcher has shut down");
            }

            if (frame.Running != null)
            {
                throw new InvalidOperationException("Frame is already running on a different dispatcher");
            }

            frame.ParentFrame = dis.CurrentFrame;
            dis.CurrentFrame = frame;

            frame.Running = dis;

            dis.RunFrame(frame);
        }


        public static void Run()
        {
            PushFrame(mainExecutionFrame);
        }

        public abstract DispatcherOperation BeginInvoke(Action method);

        public abstract DispatcherOperation BeginInvoke(DispatcherPriority priority, Action method);

        protected internal abstract void Reprioritize(DispatcherOperation op, DispatcherPriority oldpriority);

        protected abstract void RunFrame(DispatcherFrame frame);
    }
}
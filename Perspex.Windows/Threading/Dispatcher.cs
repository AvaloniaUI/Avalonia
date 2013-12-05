// -----------------------------------------------------------------------
// <copyright file="Dispatcher.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Windows.Threading
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Security;
    using System.Threading;
    using Perspex.Windows.Interop;

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

    public sealed class Dispatcher
    {
        private const int TopPriority = (int)DispatcherPriority.Send;

        private static Dictionary<Thread, Dispatcher> dispatchers = new Dictionary<Thread, Dispatcher>();

        private static object olock = new object();

        private static DispatcherFrame mainExecutionFrame = new DispatcherFrame();

        private Thread baseThread;

        private PokableQueue[] priorityQueues = new PokableQueue[TopPriority + 1];

        private Flags flags;

        private int queueBits;

        private DispatcherFrame currentFrame;

        private Dispatcher(Thread t)
        {
            this.baseThread = t;

            for (int i = 1; i <= (int)DispatcherPriority.Send; i++)
            {
                this.priorityQueues[i] = new PokableQueue();
            }
        }

        public event EventHandler ShutdownStarted;

        public event EventHandler ShutdownFinished;

        public static Dispatcher CurrentDispatcher
        {
            get
            {
                lock (olock)
                {
                    Thread t = Thread.CurrentThread;
                    Dispatcher dis = FromThread(t);

                    if (dis != null)
                    {
                        return dis;
                    }

                    dis = new Dispatcher(t);
                    dispatchers[t] = dis;
                    return dis;
                }
            }
        }

        public Thread Thread
        {
            get
            {
                return this.baseThread;
            }
        }

        public bool HasShutdownStarted
        {
            get
            {
                return (this.flags & Flags.ShutdownStarted) != 0;
            }
        }

        public bool HasShutdownFinished
        {
            get
            {
                return (this.flags & Flags.Shutdown) != 0;
            }
        }

        [SecurityCritical]
        public static void ExitAllFrames()
        {
            Dispatcher dis = CurrentDispatcher;

            for (DispatcherFrame frame = dis.currentFrame; frame != null; frame = frame.ParentFrame)
            {
                if (frame.ExitOnRequest)
                {
                    frame.Continue = false;
                }
                else
                {
                    break;
                }
            }
        }

        public static Dispatcher FromThread(Thread thread)
        {
            Dispatcher dis;

            if (dispatchers.TryGetValue(thread, out dis))
            {
                return dis;
            }

            return null;
        }

        [SecurityCritical]
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

            if ((dis.flags & Flags.Disabled) != 0)
            {
                throw new InvalidOperationException("Dispatcher processing has been disabled");
            }

            frame.ParentFrame = dis.currentFrame;
            dis.currentFrame = frame;

            frame.Running = dis;

            dis.RunFrame(frame);
        }

        [SecurityCritical]
        public static void Run()
        {
            PushFrame(mainExecutionFrame);
        }

        public static void ValidatePriority(DispatcherPriority priority, string parameterName)
        {
            if (priority < DispatcherPriority.Inactive || priority > DispatcherPriority.Send)
            {
                throw new InvalidEnumArgumentException(parameterName);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool CheckAccess()
        {
            return Thread.CurrentThread == this.baseThread;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void VerifyAccess()
        {
            if (Thread.CurrentThread != this.baseThread)
            {
                throw new InvalidOperationException("Invoked from a different thread");
            }
        }

        public DispatcherOperation BeginInvoke(Delegate method, params object[] args)
        {
            throw new NotImplementedException();
        }

        public DispatcherOperation BeginInvoke(Delegate method, DispatcherPriority priority, params object[] args)
        {
            throw new NotImplementedException();
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DispatcherOperation BeginInvoke(DispatcherPriority priority, Delegate method)
        {
            if (priority < 0 || priority > DispatcherPriority.Send)
            {
                throw new InvalidEnumArgumentException("priority");
            }

            if (priority == DispatcherPriority.Inactive)
            {
                throw new ArgumentException("priority can not be inactive", "priority");
            }

            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            DispatcherOperation op = new DispatcherOperation(this, priority, method);
            this.Queue(priority, op);

            return op;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DispatcherOperation BeginInvoke(DispatcherPriority priority, Delegate method, object arg)
        {
            if (priority < 0 || priority > DispatcherPriority.Send)
            {
                throw new InvalidEnumArgumentException("priority");
            }

            if (priority == DispatcherPriority.Inactive)
            {
                throw new ArgumentException("priority can not be inactive", "priority");
            }

            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            DispatcherOperation op = new DispatcherOperation(this, priority, method, arg);

            this.Queue(priority, op);

            return op;
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DispatcherOperation BeginInvoke(DispatcherPriority priority, Delegate method, object arg, params object[] args)
        {
            if (priority < 0 || priority > DispatcherPriority.Send)
            {
                throw new InvalidEnumArgumentException("priority");
            }

            if (priority == DispatcherPriority.Inactive)
            {
                throw new ArgumentException("priority can not be inactive", "priority");
            }

            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            DispatcherOperation op = new DispatcherOperation(this, priority, method, arg, args);
            this.Queue(priority, op);

            return op;
        }

        public object Invoke(Delegate method, params object[] args)
        {
            throw new NotImplementedException();
        }

        public object Invoke(Delegate method, TimeSpan timeout, params object[] args)
        {
            throw new NotImplementedException();
        }

        public object Invoke(Delegate method, TimeSpan timeout, DispatcherPriority priority, params object[] args)
        {
            throw new NotImplementedException();
        }

        public object Invoke(Delegate method, DispatcherPriority priority, params object[] args)
        {
            throw new NotImplementedException();
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object Invoke(DispatcherPriority priority, Delegate method)
        {
            if (priority < 0 || priority > DispatcherPriority.Send)
            {
                throw new InvalidEnumArgumentException("priority");
            }

            if (priority == DispatcherPriority.Inactive)
            {
                throw new ArgumentException("priority can not be inactive", "priority");
            }

            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            DispatcherOperation op = new DispatcherOperation(this, priority, method);
            this.Queue(priority, op);
            PushFrame(new DispatcherFrame());

            throw new NotImplementedException();
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object Invoke(DispatcherPriority priority, Delegate method, object arg)
        {
            if (priority < 0 || priority > DispatcherPriority.Send)
            {
                throw new InvalidEnumArgumentException("priority");
            }

            if (priority == DispatcherPriority.Inactive)
            {
                throw new ArgumentException("priority can not be inactive", "priority");
            }

            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            this.Queue(priority, new DispatcherOperation(this, priority, method, arg));
            throw new NotImplementedException();
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object Invoke(DispatcherPriority priority, Delegate method, object arg, params object[] args)
        {
            if (priority < 0 || priority > DispatcherPriority.Send)
            {
                throw new InvalidEnumArgumentException("priority");
            }

            if (priority == DispatcherPriority.Inactive)
            {
                throw new ArgumentException("priority can not be inactive", "priority");
            }

            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            this.Queue(priority, new DispatcherOperation(this, priority, method, arg, args));

            throw new NotImplementedException();
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object Invoke(DispatcherPriority priority, TimeSpan timeout, Delegate method)
        {
            throw new NotImplementedException();
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object Invoke(DispatcherPriority priority, TimeSpan timeout, Delegate method, object arg)
        {
            throw new NotImplementedException();
        }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object Invoke(DispatcherPriority priority, TimeSpan timeout, Delegate method, object arg, params object[] args)
        {
            throw new NotImplementedException();
        }

        [SecurityCritical]
        public void InvokeShutdown()
        {
            this.flags |= Flags.ShutdownStarted;

            UnmanagedMethods.PostMessage(
                IntPtr.Zero,
                (int)UnmanagedMethods.WindowsMessage.WM_DISPATCH_WORK_ITEM,
                IntPtr.Zero,
                IntPtr.Zero);
        }

        [SecurityCritical]
        public void BeginInvokeShutdown(DispatcherPriority priority)
        {
            throw new NotImplementedException();
        }

        internal void Reprioritize(DispatcherOperation op, DispatcherPriority oldpriority)
        {
            int oldp = (int)oldpriority;
            PokableQueue q = this.priorityQueues[oldp];

            lock (q)
            {
                q.Remove(op);
            }

            this.Queue(op.Priority, op);
        }

        private void Queue(DispatcherPriority priority, DispatcherOperation x)
        {
            int p = (int)priority;
            PokableQueue q = this.priorityQueues[p];

            lock (q)
            {
                int flag = 1 << p;
                q.Enqueue(x);
                this.queueBits |= flag;
            }

            if (Thread.CurrentThread != this.baseThread)
            {
                UnmanagedMethods.PostMessage(
                    IntPtr.Zero,
                    (int)UnmanagedMethods.WindowsMessage.WM_DISPATCH_WORK_ITEM,
                    IntPtr.Zero,
                    IntPtr.Zero);
            }
        }

        private void PerformShutdown()
        {
            EventHandler h;

            h = this.ShutdownStarted;
            if (h != null)
            {
                h(this, new EventArgs());
            }

            this.flags |= Flags.Shutdown;

            h = this.ShutdownFinished;
            if (h != null)
            {
                h(this, new EventArgs());
            }

            this.priorityQueues = null;
        }

        private void RunFrame(DispatcherFrame frame)
        {
            do
            {
                while (this.queueBits != 0)
                {
                    for (int i = TopPriority; i > 0 && this.queueBits != 0; i--)
                    {
                        int currentBit = this.queueBits & (1 << i);
                        if (currentBit != 0)
                        {
                            PokableQueue q = this.priorityQueues[i];

                            do
                            {
                                DispatcherOperation task;

                                lock (q)
                                {
                                    task = (DispatcherOperation)q.Dequeue();
                                }

                                task.Invoke();

                                if (!frame.Continue)
                                {
                                    return;
                                }

                                if (this.HasShutdownStarted)
                                {
                                    this.PerformShutdown();
                                    return;
                                }

                                lock (q)
                                {
                                    if (q.Count == 0)
                                    {
                                        this.queueBits &= ~(1 << i);
                                        break;
                                    }
                                }

                                if (currentBit < (this.queueBits & ~currentBit))
                                {
                                    break;
                                }
                            }
                            while (true);
                        }
                    }
                }

                UnmanagedMethods.MSG msg;
                UnmanagedMethods.GetMessage(out msg, IntPtr.Zero, 0, 0);
                UnmanagedMethods.TranslateMessage(ref msg);
                UnmanagedMethods.DispatchMessage(ref msg);

                if (this.HasShutdownStarted)
                {
                    this.PerformShutdown();
                    return;
                }
            }
            while (frame.Continue);
        }

        private class PokableQueue
        {
            private const int InitialCapacity = 32;

            private int size, head, tail;
            private object[] array;

            internal PokableQueue(int capacity)
            {
                this.array = new object[capacity];
            }

            internal PokableQueue()
                : this(InitialCapacity)
            {
            }

            public int Count
            {
                get
                {
                    return this.size;
                }
            }

            public void Enqueue(object obj)
            {
                if (this.size == this.array.Length)
                {
                    this.Grow();
                }

                this.array[this.tail] = obj;
                this.tail = (this.tail + 1) % this.array.Length;
                this.size++;
            }

            public object Dequeue()
            {
                if (this.size < 1)
                {
                    throw new InvalidOperationException();
                }

                object result = this.array[this.head];
                this.array[this.head] = null;
                this.head = (this.head + 1) % this.array.Length;
                this.size--;
                return result;
            }

            public void Remove(object obj)
            {
                for (int i = 0; i < this.size; i++)
                {
                    if (this.array[(this.head + i) % this.array.Length] == obj)
                    {
                        for (int j = i; j < this.size - i; j++)
                        {
                            this.array[(this.head + j) % this.array.Length] = this.array[(this.head + j + 1) % this.array.Length];
                        }

                        this.size--;
                        if (this.size < 0)
                        {
                            this.size = this.array.Length - 1;
                        }

                        this.tail--;
                    }
                }
            }

            private void Grow()
            {
                int newc = this.array.Length * 2;
                object[] newContents = new object[newc];
                this.array.CopyTo(newContents, 0);
                this.array = newContents;
                this.head = 0;
                this.tail = this.head + this.size;
            }
        }
    }
}
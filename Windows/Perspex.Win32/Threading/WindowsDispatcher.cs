// -----------------------------------------------------------------------
// <copyright file="WindowsDispatcher.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Win32.Threading
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Security;
    using System.Threading;
    using Perspex.Threading;
    using Perspex.Win32.Interop;

    [Flags]
    internal enum Flags
    {
        ShutdownStarted = 1,
        Shutdown = 2,
        Disabled = 4
    }

    public sealed class WindowsDispatcher : Dispatcher
    {
        private const int TopPriority = (int)DispatcherPriority.Send;

        private static Dictionary<Thread, WindowsDispatcher> dispatchers = new Dictionary<Thread, WindowsDispatcher>();

        private static object olock = new object();

        private Thread baseThread;

        private PokableQueue[] priorityQueues = new PokableQueue[TopPriority + 1];

        private Flags flags;

        private int queueBits;

        private WindowsDispatcher(Thread t)
        {
            this.baseThread = t;

            for (int i = 1; i <= (int)DispatcherPriority.Send; i++)
            {
                this.priorityQueues[i] = new PokableQueue();
            }
        }

        public event EventHandler ShutdownStarted;

        public event EventHandler ShutdownFinished;

        public override DispatcherFrame CurrentFrame
        {
            get;
            set;
        }

        public Thread Thread
        {
            get { return this.baseThread; }
        }

        public bool HasShutdownStarted
        {
            get { return (this.flags & Flags.ShutdownStarted) != 0; }
        }

        public override bool HasShutdownFinished
        {
            get { return (this.flags & Flags.Shutdown) != 0; }
        }

        [SecurityCritical]
        public static void ExitAllFrames()
        {
            Dispatcher dis = CurrentDispatcher;

            for (DispatcherFrame frame = dis.CurrentFrame; frame != null; frame = frame.ParentFrame)
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

        public static WindowsDispatcher FromThread(Thread thread)
        {
            WindowsDispatcher dis;

            if (dispatchers.TryGetValue(thread, out dis))
            {
                return dis;
            }

            return null;
        }

        public static WindowsDispatcher GetThreadDispatcher()
        {
            lock (olock)
            {
                Thread t = Thread.CurrentThread;
                WindowsDispatcher dis = FromThread(t);

                if (dis != null)
                {
                    return dis;
                }

                dis = new WindowsDispatcher(t);
                dispatchers[t] = dis;
                return dis;
            }
        }

        public override DispatcherOperation BeginInvoke(Action method)
        {
            return this.BeginInvoke(DispatcherPriority.Normal, method);
        }

        public override DispatcherOperation BeginInvoke(DispatcherPriority priority, Action method)
        {
            if (priority < DispatcherPriority.Inactive || priority > DispatcherPriority.Send)
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

        protected override void Reprioritize(DispatcherOperation op, DispatcherPriority oldpriority)
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

        protected override void RunFrame(DispatcherFrame frame)
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
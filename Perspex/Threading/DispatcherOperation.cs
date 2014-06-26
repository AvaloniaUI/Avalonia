// -----------------------------------------------------------------------
// <copyright file="DispatcherOperation.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Threading
{
    using System;
    using System.Security;

    public enum DispatcherOperationStatus
    {
        Pending = 0,
        Aborted = 1,
        Completed = 2,
        Executing = 3
    }

    public sealed class DispatcherOperation
    {
        private DispatcherOperationStatus status;
        private DispatcherPriority priority;
        private Dispatcher dispatcher;
        private object result;
        private Delegate delegateMethod;
        private object[] delegateArgs;

        public DispatcherOperation(Dispatcher dis, DispatcherPriority prio)
        {
            this.dispatcher = dis;
            this.priority = prio;
            if (this.Dispatcher.HasShutdownFinished)
            {
                this.status = DispatcherOperationStatus.Aborted;
            }
            else
            {
                this.status = DispatcherOperationStatus.Pending;
            }
        }

        public DispatcherOperation(Dispatcher dis, DispatcherPriority prio, Delegate d)
            : this(dis, prio)
        {
            this.delegateMethod = d;
        }

        public DispatcherOperation(Dispatcher dis, DispatcherPriority prio, Delegate d, object arg)
            : this(dis, prio)
        {
            this.delegateMethod = d;
            this.delegateArgs = new object[1];
            this.delegateArgs[0] = arg;
        }

        public DispatcherOperation(Dispatcher dis, DispatcherPriority prio, Delegate d, object arg, object[] args)
            : this(dis, prio)
        {
            this.delegateMethod = d;
            this.delegateArgs = new object[args.Length + 1];
            this.delegateArgs[0] = arg;
            Array.Copy(args, 1, this.delegateArgs, 0, args.Length);
        }

        public event EventHandler Completed;

        public DispatcherOperationStatus Status
        {
            get
            {
                return this.status;
            }

            internal set
            {
                this.status = value;
            }
        }

        public Dispatcher Dispatcher
        {
            get
            {
                return this.dispatcher;
            }
        }

        public DispatcherPriority Priority
        {
            get
            {
                return this.priority;
            }

            set
            {
                if (this.priority != value)
                {
                    DispatcherPriority old = this.priority;
                    this.priority = value;
                    this.dispatcher.Reprioritize(this, old);
                }
            }
        }

        public object Result
        {
            get
            {
                return this.result;
            }
        }

        public bool Abort()
        {
            this.status = DispatcherOperationStatus.Aborted;
            throw new NotImplementedException();
        }

        public DispatcherOperationStatus Wait()
        {
            if (this.status == DispatcherOperationStatus.Executing)
            {
                throw new InvalidOperationException("Already executing");
            }

            throw new NotImplementedException();
        }

        [SecurityCritical]
        public DispatcherOperationStatus Wait(TimeSpan timeout)
        {
            if (this.status == DispatcherOperationStatus.Executing)
            {
                throw new InvalidOperationException("Already executing");
            }

            throw new NotImplementedException();
        }

        public void Invoke()
        {
            this.status = DispatcherOperationStatus.Executing;

            if (this.delegateMethod != null)
            {
                this.result = this.delegateMethod.DynamicInvoke(this.delegateArgs);
            }

            this.status = DispatcherOperationStatus.Completed;

            if (this.Completed != null)
            {
                this.Completed(this, EventArgs.Empty);
            }
        }
    }
}
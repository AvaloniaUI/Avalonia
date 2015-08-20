// -----------------------------------------------------------------------
// <copyright file="TestObserver.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling.UnitTests
{
    using System;

    internal class TestObserver<T> : IObserver<T>
    {
        private bool hasValue;

        private T value;

        public bool Completed { get; private set; }

        public Exception Error { get; private set; }

        public T GetValue()
        {
            if (!this.hasValue)
            {
                throw new Exception("Observable provided no value.");
            }

            if (this.Completed)
            {
                throw new Exception("Observable completed unexpectedly.");
            }

            if (this.Error != null)
            {
                throw new Exception("Observable errored unexpectedly.");
            }

            this.hasValue = false;
            return this.value;
        }

        public void OnCompleted()
        {
            this.Completed = true;
        }

        public void OnError(Exception error)
        {
            this.Error = error;
        }

        public void OnNext(T value)
        {
            if (!this.hasValue)
            {
                this.value = value;
                this.hasValue = true;
            }
            else
            {
                throw new Exception("Observable pushed more than one value.");
            }
        }
    }
}

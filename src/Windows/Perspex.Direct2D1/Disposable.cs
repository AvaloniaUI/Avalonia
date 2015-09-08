// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Direct2D1
{
    public class Disposable<T> : IDisposable where T : IDisposable
    {
        private IDisposable _extra;

        public Disposable(T inner)
        {
            Inner = inner;
        }

        public Disposable(T inner, IDisposable extra)
        {
            Inner = inner;
            _extra = extra;
        }

        public T Inner { get; }

        public static implicit operator T(Disposable<T> i)
        {
            return i.Inner;
        }

        public void Dispose()
        {
            Inner.Dispose();
            _extra?.Dispose();
        }
    }
}

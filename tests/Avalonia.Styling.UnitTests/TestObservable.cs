// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;

namespace Avalonia.Styling.UnitTests
{
    public class TestObservable : IObservable<bool>
    {
        public int SubscribedCount { get; private set; }

        public IDisposable Subscribe(IObserver<bool> observer)
        {
            ++SubscribedCount;
            observer.OnNext(true);
            return Disposable.Create(() => --SubscribedCount);
        }
    }
}

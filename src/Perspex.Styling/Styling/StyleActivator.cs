// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Perspex.Styling
{
    public enum ActivatorMode
    {
        And,
        Or,
    }

    public class StyleActivator : ObservableBase<bool>
    {
        private readonly IObservable<bool>[] _inputs;
        private readonly ActivatorMode _mode;

        public StyleActivator(
            IList<IObservable<bool>> inputs,
            ActivatorMode mode = ActivatorMode.And)
        {
            _inputs = inputs.ToArray();
            _mode = mode;
        }

        protected override IDisposable SubscribeCore(IObserver<bool> observer)
        {
            return _inputs.CombineLatest()
                .Select(Calculate)
                .DistinctUntilChanged()
                .Subscribe(observer);
        }

        private bool Calculate(IList<bool> values)
        {
            return _mode == ActivatorMode.And ? values.All(x => x) : values.Any(x => x);
        }
    }
}

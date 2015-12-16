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

    public static class StyleActivator
    {
        public static IObservable<bool> And(IEnumerable<IObservable<bool>> inputs)
        {
            var sourceArray = inputs.Select(s => s.Publish().RefCount()).ToArray();

            var terminate = sourceArray
                .ToObservable()
                .SelectMany(x => x.LastAsync()
                .Where(y => y == false));

            return sourceArray
                .CombineLatest(values => values.All(x => x))
                .DistinctUntilChanged()
                .TakeUntil(terminate);
        }

        public static IObservable<bool> Or(IEnumerable<IObservable<bool>> inputs)
        {
            return inputs.CombineLatest()
                .Select(values => values.Any(x => x))
                .DistinctUntilChanged();
        }
    }
}

// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Avalonia.Styling
{
    public enum ActivatorMode
    {
        And,
        Or,
    }

    public static class StyleActivator
    {
        public static IObservable<bool> And(IList<IObservable<bool>> inputs)
        {
            if (inputs.Count == 0)
            {
                throw new ArgumentException("StyleActivator.And inputs may not be empty.");
            }
            else if (inputs.Count == 1)
            {
                return inputs[0];
            }
            else
            {
                return inputs.CombineLatest()
                    .Select(values => values.All(x => x))
                    .DistinctUntilChanged();
            }
        }

        public static IObservable<bool> Or(IList<IObservable<bool>> inputs)
        {
            if (inputs.Count == 0)
            {
                throw new ArgumentException("StyleActivator.Or inputs may not be empty.");
            }
            else if (inputs.Count == 1)
            {
                return inputs[0];
            }
            else
            {
                return inputs.CombineLatest()
                .Select(values => values.Any(x => x))
                .DistinctUntilChanged();
            }
        }
    }
}

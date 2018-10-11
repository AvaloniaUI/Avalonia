// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reflection;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.VisualTree;
using ReactiveUI;

namespace Avalonia
{
    public class AvaloniaActivationForViewFetcher : IActivationForViewFetcher
    {
        public int GetAffinityForView(Type view)
        {
            return typeof(IVisual).GetTypeInfo().IsAssignableFrom(view.GetTypeInfo()) ? 10 : 0;
        }

        public IObservable<bool> GetActivationForView(IActivatable view)
        {
            if (!(view is IVisual visual)) return Observable.Return(false);
             var viewLoaded = Observable
                .FromEventPattern<VisualTreeAttachmentEventArgs>(
                    x => visual.AttachedToVisualTree += x,
                    x => visual.DetachedFromVisualTree -= x)
                .Select(args => true);
             var viewUnloaded = Observable
                .FromEventPattern<VisualTreeAttachmentEventArgs>(
                    x => visual.DetachedFromVisualTree += x,
                    x => visual.DetachedFromVisualTree -= x)
                .Select(args => false);
             return viewLoaded
                .Merge(viewUnloaded)
                .DistinctUntilChanged();
        }
    }
}
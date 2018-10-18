// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reflection;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.VisualTree;
using Avalonia.Controls;
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
            if (view is WindowBase window) return GetActivationForWindowBase(window);
            return GetActivationForVisual(visual);
        }

        private IObservable<bool> GetActivationForWindowBase(WindowBase window) 
        {
            var windowLoaded = Observable
                .FromEventPattern(
                    x => window.Initialized += x,
                    x => window.Initialized -= x)
                .Select(args => true);
            var windowUnloaded = Observable
                .FromEventPattern(
                    x => window.Closed += x,
                    x => window.Closed -= x)
                .Select(args => false);
            return windowLoaded
                .Merge(windowUnloaded)
                .DistinctUntilChanged();
        }

        private IObservable<bool> GetActivationForVisual(IVisual visual) 
        {
            var visualLoaded = Observable
                .FromEventPattern<VisualTreeAttachmentEventArgs>(
                    x => visual.AttachedToVisualTree += x,
                    x => visual.AttachedToVisualTree -= x)
                .Select(args => true);
            var visualUnloaded = Observable
                .FromEventPattern<VisualTreeAttachmentEventArgs>(
                    x => visual.DetachedFromVisualTree += x,
                    x => visual.DetachedFromVisualTree -= x)
                .Select(args => false);
            return visualLoaded
                .Merge(visualUnloaded)
                .DistinctUntilChanged();
        }
    }
}
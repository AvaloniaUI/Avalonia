using System;
using System.Reactive.Linq;
using Avalonia.VisualTree;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ReactiveUI;

namespace Avalonia.ReactiveUI
{
    /// <summary>
    /// Determines when Avalonia IVisuals get activated.
    /// </summary>
    public class AvaloniaActivationForViewFetcher : IActivationForViewFetcher
    {
        /// <summary>
        /// Returns affinity for view.
        /// </summary>
        public int GetAffinityForView(Type view)
        {
            return typeof(Visual).IsAssignableFrom(view) ? 10 : 0;
        }

        /// <summary>
        /// Returns activation observable for activatable Avalonia view.
        /// </summary>
        public IObservable<bool> GetActivationForView(IActivatableView view)
        {
            if (!(view is Visual visual)) return Observable.Return(false);
            if (view is Control control) return GetActivationForControl(control);
            return GetActivationForVisual(visual);
        }

        /// <summary>
        /// Listens to Loaded and Unloaded 
        /// events for Avalonia Control.
        /// </summary>
        private IObservable<bool> GetActivationForControl(Control control) 
        {
            var controlLoaded = Observable
                .FromEventPattern<RoutedEventArgs>(
                    x => control.Loaded += x,
                    x => control.Loaded -= x)
                .Select(args => true);
            var controlUnloaded = Observable
                .FromEventPattern<RoutedEventArgs>(
                    x => control.Unloaded += x,
                    x => control.Unloaded -= x)
                .Select(args => false);
            return controlLoaded
                .Merge(controlUnloaded)
                .DistinctUntilChanged();
        }

        /// <summary>
        /// Listens to AttachedToVisualTree and DetachedFromVisualTree 
        /// events for Avalonia IVisuals.
        /// </summary>
        private IObservable<bool> GetActivationForVisual(Visual visual) 
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

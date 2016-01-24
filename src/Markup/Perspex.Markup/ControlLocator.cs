// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using Perspex.Controls;

namespace Perspex.Markup
{
    /// <summary>
    /// Locates controls relative to other controls.
    /// </summary>
    public static class ControlLocator
    {
        /// <summary>
        /// Tracks a named control relative to another control.
        /// </summary>
        /// <param name="relativeTo">
        /// The control relative from which the other control should be found.
        /// </param>
        /// <param name="name">The name of the control to find.</param>
        public static IObservable<IControl> Track(IControl relativeTo, string name)
        {
            var attached = Observable.FromEventPattern<VisualTreeAttachmentEventArgs>(
                x => relativeTo.AttachedToVisualTree += x,
                x => relativeTo.DetachedFromVisualTree += x)
                .Select(x => ((IControl)x.Sender).FindNameScope())
                .StartWith(relativeTo.FindNameScope());

            var detached = Observable.FromEventPattern<VisualTreeAttachmentEventArgs>(
                x => relativeTo.DetachedFromVisualTree += x,
                x => relativeTo.DetachedFromVisualTree += x)
                .Select(x => (INameScope)null);

            return attached.Merge(detached).Select(nameScope =>
            {
                if (nameScope != null)
                {
                    var registered = Observable.FromEventPattern<NameScopeEventArgs>(
                        x => nameScope.Registered += x,
                        x => nameScope.Registered -= x)
                        .Where(x => x.EventArgs.Name == name)
                        .Select(x => x.EventArgs.Element)
                        .OfType<IControl>();
                    var unregistered = Observable.FromEventPattern<NameScopeEventArgs>(
                        x => nameScope.Unregistered += x,
                        x => nameScope.Unregistered -= x)
                        .Where(x => x.EventArgs.Name == name)
                        .Select(_ => (IControl)null);
                    return registered
                        .StartWith(nameScope.Find<IControl>(name))
                        .Merge(unregistered);
                }
                else
                {
                    return Observable.Return<IControl>(null);
                }
            }).Switch();
        }
    }
}

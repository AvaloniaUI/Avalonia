// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avalonia.Markup
{
    /// <summary>
    /// The type of tree via which to track a control.
    /// </summary>
    public enum TreeType
    {
        /// <summary>
        /// The visual tree.
        /// </summary>
        Visual,
        /// <summary>
        /// The logical tree.
        /// </summary>
        Logical,
    }

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
            var attached = Observable.FromEventPattern<LogicalTreeAttachmentEventArgs>(
                x => relativeTo.AttachedToLogicalTree += x,
                x => relativeTo.AttachedToLogicalTree -= x)
                .Select(x => ((IControl)x.Sender).FindNameScope())
                .StartWith(relativeTo.FindNameScope());

            var detached = Observable.FromEventPattern<LogicalTreeAttachmentEventArgs>(
                x => relativeTo.DetachedFromLogicalTree += x,
                x => relativeTo.DetachedFromLogicalTree -= x)
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

        /// <summary>
        /// Tracks a typed visual ancestor control.
        /// </summary>
        /// <param name="relativeTo">
        /// The control relative from which the other control should be found.
        /// </param>
        /// <param name="tree">The tree via which to track the control.</param>
        /// <param name="ancestorLevel">
        /// The level of ancestor control to look for. Use 0 for the first ancestor of the
        /// requested type.
        /// </param>
        /// <param name="ancestorType">The type of the ancestor to find.</param>
        public static IObservable<IControl> Track(IControl relativeTo, TreeType tree, int ancestorLevel, Type ancestorType = null)
        {
            return TrackAttachmentToTree(relativeTo, tree).Select(isAttachedToTree =>
            {
                if (isAttachedToTree)
                {
                    if (tree == TreeType.Visual)
                    {
                        return relativeTo.GetVisualAncestors()
                            .Where(x => ancestorType?.GetTypeInfo().IsAssignableFrom(x.GetType().GetTypeInfo()) ?? true)
                            .ElementAtOrDefault(ancestorLevel) as IControl; 
                    }
                    else
                    {
                        return relativeTo.GetLogicalAncestors()
                            .Where(x => ancestorType?.GetTypeInfo().IsAssignableFrom(x.GetType().GetTypeInfo()) ?? true)
                            .ElementAtOrDefault(ancestorLevel) as IControl;
                    }
                }
                else
                {
                    return null;
                }
            });
        }

        private static IObservable<bool> TrackAttachmentToTree(IControl relativeTo, TreeType tree)
        {
            return tree == TreeType.Visual ? TrackAttachmentToVisualTree(relativeTo) : TrackAttachmentToLogicalTree(relativeTo);
        }

        private static IObservable<bool> TrackAttachmentToVisualTree(IControl relativeTo)
        {
            var attached = Observable.FromEventPattern<VisualTreeAttachmentEventArgs>(
                x => relativeTo.AttachedToVisualTree += x,
                x => relativeTo.AttachedToVisualTree -= x)
                .Select(x => true)
                .StartWith(relativeTo.IsAttachedToVisualTree);

            var detached = Observable.FromEventPattern<VisualTreeAttachmentEventArgs>(
                x => relativeTo.DetachedFromVisualTree += x,
                x => relativeTo.DetachedFromVisualTree -= x)
                .Select(x => false);

            var attachmentStatus = attached.Merge(detached);
            return attachmentStatus;
        }

        private static IObservable<bool> TrackAttachmentToLogicalTree(IControl relativeTo)
        {
            var attached = Observable.FromEventPattern<LogicalTreeAttachmentEventArgs>(
                x => relativeTo.AttachedToLogicalTree += x,
                x => relativeTo.AttachedToLogicalTree -= x)
                .Select(x => true)
                .StartWith(relativeTo.IsAttachedToLogicalTree);

            var detached = Observable.FromEventPattern<LogicalTreeAttachmentEventArgs>(
                x => relativeTo.DetachedFromLogicalTree += x,
                x => relativeTo.DetachedFromLogicalTree -= x)
                .Select(x => false);

            var attachmentStatus = attached.Merge(detached);
            return attachmentStatus;
        }
    }
}

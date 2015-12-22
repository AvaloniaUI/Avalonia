// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Perspex.Collections;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Interactivity;

namespace Perspex.Controls.Mixins
{
    /// <summary>
    /// Adds content control functionality to control classes.
    /// </summary>
    /// <para>
    /// The <see cref="ContentControlMixin"/> adds behavior to a control which acts as a content
    /// control such as <see cref="ContentControl"/> and <see cref="HeaderedItemsControl"/>. It
    /// updates keeps the control's logical children in sync with the content being displayed by
    /// the control.
    /// </para>
    public class ContentControlMixin
    {
        private static Lazy<ConditionalWeakTable<TemplatedControl, IDisposable>> subscriptions = 
            new Lazy<ConditionalWeakTable<TemplatedControl, IDisposable>>(() => 
                new ConditionalWeakTable<TemplatedControl, IDisposable>());

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectableMixin"/> class.
        /// </summary>
        /// <typeparam name="TControl">The control type.</typeparam>
        /// <param name="content">The content property.</param>
        /// <param name="logicalChildrenSelector">
        /// Given an control of <typeparamref name="TControl"/> should return the control's
        /// logical children collection.
        /// </param>
        /// <param name="presenterName">
        /// The name of the content presenter in the control's template.
        /// </param>
        public static void Attach<TControl>(
            PerspexProperty content,            
            Func<TControl, IPerspexList<ILogical>> logicalChildrenSelector,
            string presenterName = "PART_ContentPresenter")
            where TControl : TemplatedControl
        {
            Contract.Requires<ArgumentNullException>(content != null);
            Contract.Requires<ArgumentNullException>(logicalChildrenSelector != null);

            EventHandler<RoutedEventArgs> templateApplied = (s, ev) =>
            {
                var sender = s as TControl;

                if (sender != null)
                {
                    var e = (TemplateAppliedEventArgs)ev;
                    var presenter = (IControl)e.NameScope.Find(presenterName);

                    if (presenter != null)
                    {
                        var logicalChildren = logicalChildrenSelector(sender);
                        var subscription = presenter
                            .GetObservable(ContentPresenter.ChildProperty)
                            .Subscribe(child => UpdateLogicalChild(
                                logicalChildren, 
                                logicalChildren.FirstOrDefault(), 
                                child));
                        subscriptions.Value.Add(sender, subscription);
                    }
                }
            };

            TemplatedControl.TemplateAppliedEvent.AddClassHandler(
                typeof(TControl),
                templateApplied,
                RoutingStrategies.Direct);

            content.Changed.Subscribe(e =>
            {
                var sender = e.Sender as TControl;

                if (sender != null)
                {
                    var logicalChildren = logicalChildrenSelector(sender);
                    UpdateLogicalChild(logicalChildren, e.OldValue, e.NewValue);
                }
            });

            TemplatedControl.TemplateProperty.Changed.Subscribe(e =>
            {
                var sender = e.Sender as TControl;

                if (sender != null)
                {
                    IDisposable subscription;

                    if (subscriptions.Value.TryGetValue(sender, out subscription))
                    {
                        subscription.Dispose();
                        subscriptions.Value.Remove(sender);
                    }
                }
            });
        }

        private static event EventHandler<TemplateAppliedEventArgs> TemplateApplied;

        private static void OnTemplateApplied(object sender, RoutedEventArgs e)
        {
            TemplateApplied?.Invoke(sender, (TemplateAppliedEventArgs)e);
        }

        private static void UpdateLogicalChild(
            IPerspexList<ILogical> logicalChildren,
            object oldValue, 
            object newValue)
        {
            if (oldValue != newValue)
            {
                var logical = oldValue as ILogical;

                if (logical != null)
                {
                    logicalChildren.Remove(logical);
                }

                logical = newValue as ILogical;

                if (logical != null)
                {
                    logicalChildren.Add(logical);
                }
            }
        }
    }
}

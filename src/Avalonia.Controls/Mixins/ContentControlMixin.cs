// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;

namespace Avalonia.Controls.Mixins
{
    /// <summary>
    /// Adds content control functionality to control classes.
    /// </summary>
    /// <para>
    /// The <see cref="ContentControlMixin"/> adds behavior to a control which acts as a content
    /// control such as <see cref="ContentControl"/> and <see cref="HeaderedItemsControl"/>. It
    /// keeps the control's logical children in sync with the content being displayed by the
    /// control.
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
            AvaloniaProperty content,            
            Func<TControl, IAvaloniaList<ILogical>> logicalChildrenSelector,
            string presenterName = "PART_ContentPresenter")
            where TControl : TemplatedControl
        {
            Contract.Requires<ArgumentNullException>(content != null);
            Contract.Requires<ArgumentNullException>(logicalChildrenSelector != null);

            void ChildChanging(object s, AvaloniaPropertyChangedEventArgs e)
            {
                if (s is IControl sender && sender?.TemplatedParent is TControl parent)
                {
                    UpdateLogicalChild(
                        sender,
                        logicalChildrenSelector(parent),
                        e.OldValue,
                        null);
                }
            }

            void TemplateApplied(object s, RoutedEventArgs ev)
            {
                if (s is TControl sender)
                {
                    var e = (TemplateAppliedEventArgs)ev;
                    var presenter = e.NameScope.Find(presenterName) as IContentPresenter;

                    if (presenter != null)
                    {
                        presenter.ApplyTemplate();

                        var logicalChildren = logicalChildrenSelector(sender);
                        var subscription = new CompositeDisposable();

                        presenter.ChildChanging += ChildChanging;
                        subscription.Add(Disposable.Create(() => presenter.ChildChanging -= ChildChanging));

                        subscription.Add(presenter
                            .GetPropertyChangedObservable(ContentPresenter.ChildProperty)
                            .Subscribe(c => UpdateLogicalChild(
                                sender,
                                logicalChildren,
                                null,
                                c.NewValue)));

                        UpdateLogicalChild(
                            sender,
                            logicalChildren,
                            null,
                            presenter.GetValue(ContentPresenter.ChildProperty));

                        if (subscriptions.Value.TryGetValue(sender, out IDisposable previousSubscription))
                        {
                            subscription = new CompositeDisposable(previousSubscription, subscription);
                            subscriptions.Value.Remove(sender);
                        }

                        subscriptions.Value.Add(sender, subscription);
                    }
                }
            }

            TemplatedControl.TemplateAppliedEvent.AddClassHandler(
                typeof(TControl),
                TemplateApplied,
                RoutingStrategies.Direct);

            content.Changed.Subscribe(e =>
            {
                if (e.Sender is TControl sender)
                {
                    var logicalChildren = logicalChildrenSelector(sender);
                    UpdateLogicalChild(sender, logicalChildren, e.OldValue, e.NewValue);
                }
            });

            Control.TemplatedParentProperty.Changed.Subscribe(e =>
            {
                if (e.Sender is TControl sender)
                {
                    var logicalChild = logicalChildrenSelector(sender).FirstOrDefault() as IControl;
                    logicalChild?.SetValue(Control.TemplatedParentProperty, sender.TemplatedParent);
                }
            });

            TemplatedControl.TemplateProperty.Changed.Subscribe(e =>
            {
                if (e.Sender is TControl sender)
                {
                    if (subscriptions.Value.TryGetValue(sender, out IDisposable subscription))
                    {
                        subscription.Dispose();
                        subscriptions.Value.Remove(sender);
                    }
                }
            });
        }

        private static void UpdateLogicalChild(
            IControl control,
            IAvaloniaList<ILogical> logicalChildren,
            object oldValue, 
            object newValue)
        {
            if (oldValue != newValue)
            {
                if (oldValue is IControl child)
                {
                    logicalChildren.Remove(child);
                }

                child = newValue as IControl;

                if (child != null && !logicalChildren.Contains(child))
                {
                    child.SetValue(Control.TemplatedParentProperty, control.TemplatedParent);
                    logicalChildren.Add(child);
                }
            }
        }
    }
}

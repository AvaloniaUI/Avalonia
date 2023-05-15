using System;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Diagnostics;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Avalonia.Reactive;
using Lifetimes = Avalonia.Controls.ApplicationLifetimes;
using System.Linq;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class VisualTreeNode : TreeNode
    {
        public VisualTreeNode(AvaloniaObject avaloniaObject, TreeNode? parent, string? customName = null)
            : base(avaloniaObject, parent, customName)
        {
            Children = avaloniaObject switch
            {
                Visual visual => new VisualTreeNodeCollection(this, visual),
                Controls.Application host => new ApplicationHostVisuals(this, host),
                _ => TreeNodeCollection.Empty
            };

            if (Visual is StyledElement styleable)
                IsInTemplate = styleable.TemplatedParent != null;
        }

        public bool IsInTemplate { get; }

        public override TreeNodeCollection Children { get; }

        public static VisualTreeNode[] Create(object control)
        {
            return control is AvaloniaObject visual ?
                new[] { new VisualTreeNode(visual, null) } :
                Array.Empty<VisualTreeNode>();
        }

        internal class VisualTreeNodeCollection : TreeNodeCollection
        {
            private readonly Visual _control;
            private readonly CompositeDisposable _subscriptions = new CompositeDisposable(2);

            public VisualTreeNodeCollection(TreeNode owner, Visual control)
                : base(owner)
            {
                _control = control;
            }

            public override void Dispose()
            {
                _subscriptions.Dispose();
            }

            private static IObservable<PopupRoot?>? GetHostedPopupRootObservable(Visual visual)
            {
                static IObservable<PopupRoot?> GetPopupHostObservable(
                    IPopupHostProvider popupHostProvider,
                    string? providerName = null)
                {
                    return Observable.Create<IPopupHost?>(observer =>
                        {
                            void Handler(IPopupHost? args) => observer.OnNext(args);
                            popupHostProvider.PopupHostChanged += Handler;
                            return Disposable.Create(() => popupHostProvider.PopupHostChanged -= Handler);
                        })
                        .StartWith(popupHostProvider.PopupHost)
                        .Select(popupHost =>
                        {
                            if (popupHost is Control control)
                                return new PopupRoot(
                                    control,
                                    providerName != null ? $"{providerName} ({control.GetType().Name})" : null);

                            return (PopupRoot?)null;
                        });
                }

                return visual switch
                {
                    Popup p => GetPopupHostObservable(p),
                    Control c => Observable.CombineLatest(
                            new IObservable<object?>[]
                            {
                                c.GetObservable(Control.ContextFlyoutProperty),
                                c.GetObservable(Control.ContextMenuProperty),
                                c.GetObservable(FlyoutBase.AttachedFlyoutProperty),
                                c.GetObservable(ToolTipDiagnostics.ToolTipProperty),
                                c.GetObservable(Button.FlyoutProperty)
                            })
                        .Select(
                            items =>
                            {
                                var contextFlyout = items[0] as IPopupHostProvider;
                                var contextMenu = items[1] as ContextMenu;
                                var attachedFlyout = items[2] as IPopupHostProvider;
                                var toolTip = items[3] as IPopupHostProvider;
                                var buttonFlyout = items[4] as IPopupHostProvider;

                                if (contextMenu != null)
                                    //Note: ContextMenus are special since all the items are added as visual children.
                                    //So we don't need to go via Popup
                                    return Observable.Return<PopupRoot?>(new PopupRoot(contextMenu));

                                if (contextFlyout != null)
                                    return GetPopupHostObservable(contextFlyout, "ContextFlyout");

                                if (attachedFlyout != null)
                                    return GetPopupHostObservable(attachedFlyout, "AttachedFlyout");

                                if (toolTip != null)
                                    return GetPopupHostObservable(toolTip, "ToolTip");

                                if (buttonFlyout != null)
                                    return GetPopupHostObservable(buttonFlyout, "Flyout");

                                return Observable.Return<PopupRoot?>(null);
                            })
                        .Switch(),
                    _ => null
                };
            }

            protected override void Initialize(AvaloniaList<TreeNode> nodes)
            {
                _subscriptions.Clear();

                if (GetHostedPopupRootObservable(_control) is { } popupRootObservable)
                {
                    VisualTreeNode? childNode = null;

                    _subscriptions.Add(
                        popupRootObservable
                            .Subscribe(popupRoot =>
                            {
                                if (popupRoot != null)
                                {
                                    childNode = new VisualTreeNode(
                                        popupRoot.Value.Root,
                                        Owner,
                                        popupRoot.Value.CustomName);

                                    nodes.Add(childNode);
                                }
                                else if (childNode != null)
                                {
                                    nodes.Remove(childNode);
                                }
                            }));
                }

                _subscriptions.Add(
                    _control.VisualChildren.ForEachItem(
                        (i, item) => nodes.Insert(i, new VisualTreeNode((AvaloniaObject)item, Owner)),
                        (i, item) => nodes.RemoveAt(i),
                        () => nodes.Clear()));
            }

            private struct PopupRoot
            {
                public PopupRoot(Control root, string? customName = null)
                {
                    Root = root;
                    CustomName = customName;
                }

                public Control Root { get; }
                public string? CustomName { get; }
            }
        }

        internal class ApplicationHostVisuals : TreeNodeCollection
        {
            readonly Controls.Application _application;
            CompositeDisposable _subscriptions = new CompositeDisposable(2);
            public ApplicationHostVisuals(TreeNode owner, Controls.Application host) :
                base(owner)
            {
                _application = host;
            }

            protected override void Initialize(AvaloniaList<TreeNode> nodes)
            {
                if (_application.ApplicationLifetime is Lifetimes.ISingleViewApplicationLifetime single &&
                    single.MainView is not null)
                {
                    nodes.Add(new VisualTreeNode(single.MainView, Owner));
                }
                if (_application.ApplicationLifetime is Lifetimes.IClassicDesktopStyleApplicationLifetime classic)
                {

                    for (int i = 0; i < classic.Windows.Count; i++)
                    {
                        var window = classic.Windows[i];
                        if (window is Views.MainWindow)
                        {
                            continue;
                        }
                        nodes.Add(new VisualTreeNode(window, Owner));
                    }
                    _subscriptions = new CompositeDisposable(2)
                    {
                        Window.WindowOpenedEvent.AddClassHandler(typeof(Window), (s,e)=>
                            {
                                if (s is Views.MainWindow)
                                {
                                    return;
                                }
                                nodes.Add(new VisualTreeNode((AvaloniaObject)s!,Owner));
                            }),
                        Window.WindowClosedEvent.AddClassHandler(typeof(Window), (s,e)=>
                            {
                                if (s is Views.MainWindow)
                                {
                                    return;
                                }
                                var item = nodes.FirstOrDefault(node=>object.ReferenceEquals(node.Visual,s));
                                if(!(item is null))
                                {
                                    nodes.Remove(item);
                                }
                            }),
                    };


                }
            }

            public override void Dispose()
            {
                _subscriptions?.Dispose();
                base.Dispose();
            }
        }
    }
}

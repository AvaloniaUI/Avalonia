using System;
using System.Reactive.Disposables;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Lifetimes = Avalonia.Controls.ApplicationLifetimes;
using System.Linq;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class LogicalTreeNode : TreeNode
    {
        public LogicalTreeNode(AvaloniaObject avaloniaObject, TreeNode? parent)
            : base(avaloniaObject, parent)
        {
            Children =  avaloniaObject switch
            {
                ILogical logical => new LogicalTreeNodeCollection(this, logical),
                Controls.Application host => new ApplicationHostLogical(this, host),
                _ => TreeNodeCollection.Empty
            };
        }

        public override TreeNodeCollection Children { get; }

        public static LogicalTreeNode[] Create(object control)
        {
            var logical = control as AvaloniaObject;
            return logical != null ? new[] { new LogicalTreeNode(logical, null) } : Array.Empty<LogicalTreeNode>();
        }

        internal class LogicalTreeNodeCollection : TreeNodeCollection
        {
            private readonly ILogical _control;
            private IDisposable? _subscription;

            public LogicalTreeNodeCollection(TreeNode owner, ILogical control)
                : base(owner)
            {
                _control = control;
            }

            public override void Dispose()
            {
                base.Dispose();
                _subscription?.Dispose();
            }

            protected override void Initialize(AvaloniaList<TreeNode> nodes)
            {
                _subscription = _control.LogicalChildren.ForEachItem(
                    (i, item) => nodes.Insert(i, new LogicalTreeNode((AvaloniaObject)item, Owner)),
                    (i, item) => nodes.RemoveAt(i),
                    () => nodes.Clear());
            }
        }

        internal class ApplicationHostLogical : TreeNodeCollection
        {
            readonly Controls.Application _application;
            CompositeDisposable _subscriptions = new CompositeDisposable(2);
            public ApplicationHostLogical(TreeNode owner, Controls.Application host) :
                base(owner)
            {
                _application = host;
            }

            protected override void Initialize(AvaloniaList<TreeNode> nodes)
            {
                if (_application.ApplicationLifetime is Lifetimes.ISingleViewApplicationLifetime single &&
                    single.MainView is not null)
                {
                    nodes.Add(new LogicalTreeNode(single.MainView, Owner));
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
                        nodes.Add(new LogicalTreeNode(window, Owner));
                    }
                    _subscriptions = new System.Reactive.Disposables.CompositeDisposable()
                    {
                        Window.WindowOpenedEvent.AddClassHandler(typeof(Window), (s,e)=>
                            {
                                if (s is Views.MainWindow)
                                {
                                    return;
                                }
                                nodes.Add(new LogicalTreeNode((AvaloniaObject)s!,Owner));
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
                                if(nodes.Count == 0)
                                {
                                    if (Avalonia.Application.Current?.ApplicationLifetime is Lifetimes.IControlledApplicationLifetime controller)
                                    {
                                        controller.Shutdown();
                                    }
                                    else
                                    {
                                        Environment.Exit(0);
                                    }
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

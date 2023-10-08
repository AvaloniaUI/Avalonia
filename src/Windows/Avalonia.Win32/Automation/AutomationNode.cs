using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Threading;
using Avalonia.Win32.Interop.Automation;
using AAP = Avalonia.Automation.Provider;

namespace Avalonia.Win32.Automation
{
    [ComVisible(true)]
    [RequiresUnreferencedCode("Requires .NET COM interop")]
    internal partial class AutomationNode : MarshalByRefObject,
        IRawElementProviderSimple,
        IRawElementProviderSimple2,
        IRawElementProviderFragment,
        IInvokeProvider
    {
        private static Dictionary<AutomationProperty, UiaPropertyId> s_propertyMap = new()
        {
            { AutomationElementIdentifiers.BoundingRectangleProperty, UiaPropertyId.BoundingRectangle },
            { AutomationElementIdentifiers.ClassNameProperty, UiaPropertyId.ClassName },
            { AutomationElementIdentifiers.NameProperty, UiaPropertyId.Name },
            { ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, UiaPropertyId.ExpandCollapseExpandCollapseState },
            { RangeValuePatternIdentifiers.IsReadOnlyProperty, UiaPropertyId.RangeValueIsReadOnly},
            { RangeValuePatternIdentifiers.MaximumProperty, UiaPropertyId.RangeValueMaximum },
            { RangeValuePatternIdentifiers.MinimumProperty, UiaPropertyId.RangeValueMinimum },
            { RangeValuePatternIdentifiers.ValueProperty, UiaPropertyId.RangeValueValue },
            { ScrollPatternIdentifiers.HorizontallyScrollableProperty, UiaPropertyId.ScrollHorizontallyScrollable },
            { ScrollPatternIdentifiers.HorizontalScrollPercentProperty, UiaPropertyId.ScrollHorizontalScrollPercent },
            { ScrollPatternIdentifiers.HorizontalViewSizeProperty, UiaPropertyId.ScrollHorizontalViewSize },
            { ScrollPatternIdentifiers.VerticallyScrollableProperty, UiaPropertyId.ScrollVerticallyScrollable },
            { ScrollPatternIdentifiers.VerticalScrollPercentProperty, UiaPropertyId.ScrollVerticalScrollPercent },
            { ScrollPatternIdentifiers.VerticalViewSizeProperty, UiaPropertyId.ScrollVerticalViewSize },
            { SelectionPatternIdentifiers.CanSelectMultipleProperty, UiaPropertyId.SelectionCanSelectMultiple },
            { SelectionPatternIdentifiers.IsSelectionRequiredProperty, UiaPropertyId.SelectionIsSelectionRequired },
            { SelectionPatternIdentifiers.SelectionProperty, UiaPropertyId.SelectionSelection },
            { SelectionItemPatternIdentifiers.IsSelectedProperty, UiaPropertyId.SelectionItemIsSelected },
            { SelectionItemPatternIdentifiers.SelectionContainerProperty, UiaPropertyId.SelectionItemSelectionContainer }
        };

        private static ConditionalWeakTable<AutomationPeer, AutomationNode> s_nodes = new();

        private readonly int[] _runtimeId;

        public AutomationNode(AutomationPeer peer)
        {
            _runtimeId = new int[] { 3, GetHashCode() };
            Peer = peer;
            s_nodes.Add(peer, this);
            peer.ChildrenChanged += OnPeerChildrenChanged;
            peer.PropertyChanged += OnPeerPropertyChanged;

            if (Peer.GetProvider<AAP.IEmbeddedRootProvider>() is { } embeddedRoot)
                embeddedRoot.FocusChanged += OnEmbeddedRootFocusChanged;
        }

        public AutomationPeer Peer { get; protected set; }

        public Rect BoundingRectangle
        {
            get => InvokeSync(() =>
            {
                if (GetRoot() is RootAutomationNode root)
                    return root.ToScreen(Peer.GetBoundingRectangle());
                return default;
            });
        }

        public virtual IRawElementProviderFragmentRoot? FragmentRoot
        {
            get => InvokeSync(() => GetRoot());
        }

        public virtual IRawElementProviderSimple? HostRawElementProvider => null;
        public ProviderOptions ProviderOptions => ProviderOptions.ServerSideProvider;

        [return: MarshalAs(UnmanagedType.IUnknown)]
        public virtual object? GetPatternProvider(int patternId)
        {
            AutomationNode? ThisIfPeerImplementsProvider<T>() => Peer.GetProvider<T>() is object ? this : null;

            return (UiaPatternId)patternId switch
            {
                UiaPatternId.ExpandCollapse => ThisIfPeerImplementsProvider<IExpandCollapseProvider>(),
                UiaPatternId.Invoke => ThisIfPeerImplementsProvider<AAP.IInvokeProvider>(),
                UiaPatternId.RangeValue => ThisIfPeerImplementsProvider<AAP.IRangeValueProvider>(),
                UiaPatternId.Scroll => ThisIfPeerImplementsProvider<AAP.IScrollProvider>(),
                UiaPatternId.ScrollItem => this,
                UiaPatternId.Selection => ThisIfPeerImplementsProvider<AAP.ISelectionProvider>(),
                UiaPatternId.SelectionItem => ThisIfPeerImplementsProvider<AAP.ISelectionItemProvider>(),
                UiaPatternId.Toggle => ThisIfPeerImplementsProvider<AAP.IToggleProvider>(),
                UiaPatternId.Value => ThisIfPeerImplementsProvider<AAP.IValueProvider>(),
                _ => null,
            };
        }

        public virtual object? GetPropertyValue(int propertyId)
        {
            return (UiaPropertyId)propertyId switch
            {
                UiaPropertyId.AcceleratorKey => InvokeSync(() => Peer.GetAcceleratorKey()),
                UiaPropertyId.AccessKey => InvokeSync(() => Peer.GetAccessKey()),
                UiaPropertyId.AutomationId => InvokeSync(() => Peer.GetAutomationId()),
                UiaPropertyId.ClassName => InvokeSync(() => Peer.GetClassName()),
                UiaPropertyId.ClickablePoint => new[] { BoundingRectangle.Center.X, BoundingRectangle.Center.Y },
                UiaPropertyId.ControlType => InvokeSync(() => ToUiaControlType(Peer.GetAutomationControlType())),
                UiaPropertyId.Culture => CultureInfo.CurrentCulture.LCID,
                UiaPropertyId.FrameworkId => "Avalonia",
                UiaPropertyId.HasKeyboardFocus => InvokeSync(() => Peer.HasKeyboardFocus()),
                UiaPropertyId.IsContentElement => InvokeSync(() => Peer.IsContentElement()),
                UiaPropertyId.IsControlElement => InvokeSync(() => Peer.IsControlElement()),
                UiaPropertyId.IsEnabled => InvokeSync(() => Peer.IsEnabled()),
                UiaPropertyId.IsKeyboardFocusable => InvokeSync(() => Peer.IsKeyboardFocusable()),
                UiaPropertyId.LocalizedControlType => InvokeSync(() => Peer.GetLocalizedControlType()),
                UiaPropertyId.Name => InvokeSync(() => Peer.GetName()),
                UiaPropertyId.ProcessId => Process.GetCurrentProcess().Id,
                UiaPropertyId.RuntimeId => _runtimeId,
                _ => null,
            };
        }

        public int[]? GetRuntimeId() => _runtimeId;

        public virtual IRawElementProviderFragment? Navigate(NavigateDirection direction)
        {
            AutomationNode? GetSibling(int direction)
            {
                var children = Peer.GetParent()?.GetChildren();

                for (var i = 0; i < (children?.Count ?? 0); ++i)
                {
                    if (ReferenceEquals(children![i], Peer))
                    {
                        var j = i + direction;
                        if (j >= 0 && j < children.Count)
                            return GetOrCreate(children[j]);
                    }
                }

                return null;
            }

            return InvokeSync(() =>
            {
                return direction switch
                {
                    NavigateDirection.Parent => GetOrCreate(Peer.GetParent()),
                    NavigateDirection.NextSibling => GetSibling(1),
                    NavigateDirection.PreviousSibling => GetSibling(-1),
                    NavigateDirection.FirstChild => GetOrCreate(Peer.GetChildren().FirstOrDefault()),
                    NavigateDirection.LastChild => GetOrCreate(Peer.GetChildren().LastOrDefault()),
                    _ => null,
                };
            });
        }

        public void SetFocus() => InvokeSync(() => Peer.SetFocus());

        [return: NotNullIfNotNull(nameof(peer))]
        public static AutomationNode? GetOrCreate(AutomationPeer? peer)
        {
            return peer is null ? null : s_nodes.GetValue(peer, Create);
        }

        public static void Release(AutomationPeer peer) => s_nodes.Remove(peer);

        IRawElementProviderSimple[]? IRawElementProviderFragment.GetEmbeddedFragmentRoots() => null;
        void IRawElementProviderSimple2.ShowContextMenu() => InvokeSync(() => Peer.ShowContextMenu());
        void IInvokeProvider.Invoke() => InvokeSync((AAP.IInvokeProvider x) => x.Invoke());

        protected void InvokeSync(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
                action();
            else
                Dispatcher.UIThread.InvokeAsync(action).Wait();
        }

        protected T InvokeSync<T>(Func<T> func)
        {
            if (Dispatcher.UIThread.CheckAccess())
                return func();
            else
                return Dispatcher.UIThread.InvokeAsync(func).Result;
        }

        protected void InvokeSync<TInterface>(Action<TInterface> action)
        {
            if (Peer.GetProvider<TInterface>() is TInterface i)
            {
                try
                {
                    InvokeSync(() => action(i));
                }
                catch (AggregateException e) when (e.InnerException is ElementNotEnabledException)
                {
                    throw new COMException(e.Message, UiaCoreProviderApi.UIA_E_ELEMENTNOTENABLED);
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        protected TResult InvokeSync<TInterface, TResult>(Func<TInterface, TResult> func)
        {
            if (Peer.GetProvider<TInterface>() is TInterface i)
            {
                try
                {
                    return InvokeSync(() => func(i));
                }
                catch (AggregateException e) when (e.InnerException is ElementNotEnabledException)
                {
                    throw new COMException(e.Message, UiaCoreProviderApi.UIA_E_ELEMENTNOTENABLED);
                }
            }

            throw new NotSupportedException();
        }

        protected void RaiseChildrenChanged()
        {
            UiaCoreProviderApi.UiaRaiseStructureChangedEvent(
                this,
                StructureChangeType.ChildrenInvalidated,
                null,
                0);
        }

        protected void RaiseFocusChanged(AutomationNode? focused)
        {
            UiaCoreProviderApi.UiaRaiseAutomationEvent(
                focused,
                (int)UiaEventId.AutomationFocusChanged);
        }

        private RootAutomationNode? GetRoot()
        {
            Dispatcher.UIThread.VerifyAccess();
            return GetOrCreate(Peer.GetVisualRoot()) as RootAutomationNode;
        }

        private void OnPeerChildrenChanged(object? sender, EventArgs e)
        {
            RaiseChildrenChanged();
        }

        private void OnPeerPropertyChanged(object? sender, AutomationPropertyChangedEventArgs e)
        {
            if (s_propertyMap.TryGetValue(e.Property, out var id))
            {
                UiaCoreProviderApi.UiaRaiseAutomationPropertyChangedEvent(
                    this,
                    (int)id,
                    e.OldValue as IConvertible,
                    e.NewValue as IConvertible);
            }
        }

        private void OnEmbeddedRootFocusChanged(object? sender, EventArgs e)
        {
            if (Peer.GetProvider<AAP.IEmbeddedRootProvider>() is { } embeddedRoot)
                RaiseFocusChanged(GetOrCreate(embeddedRoot.GetFocus()));
        }

        private static AutomationNode Create(AutomationPeer peer)
        {
            return peer.GetProvider<AAP.IRootProvider>() is object ?
                new RootAutomationNode(peer) :
                new AutomationNode(peer);
        }

        private static UiaControlTypeId ToUiaControlType(AutomationControlType role)
        {
            return role switch
            {
                AutomationControlType.None => UiaControlTypeId.Group,
                AutomationControlType.Button => UiaControlTypeId.Button,
                AutomationControlType.Calendar => UiaControlTypeId.Calendar,
                AutomationControlType.CheckBox => UiaControlTypeId.CheckBox,
                AutomationControlType.ComboBox => UiaControlTypeId.ComboBox,
                AutomationControlType.ComboBoxItem => UiaControlTypeId.ListItem,
                AutomationControlType.Edit => UiaControlTypeId.Edit,
                AutomationControlType.Hyperlink => UiaControlTypeId.Hyperlink,
                AutomationControlType.Image => UiaControlTypeId.Image,
                AutomationControlType.ListItem => UiaControlTypeId.ListItem,
                AutomationControlType.List => UiaControlTypeId.List,
                AutomationControlType.Menu => UiaControlTypeId.Menu,
                AutomationControlType.MenuBar => UiaControlTypeId.MenuBar,
                AutomationControlType.MenuItem => UiaControlTypeId.MenuItem,
                AutomationControlType.ProgressBar => UiaControlTypeId.ProgressBar,
                AutomationControlType.RadioButton => UiaControlTypeId.RadioButton,
                AutomationControlType.ScrollBar => UiaControlTypeId.ScrollBar,
                AutomationControlType.Slider => UiaControlTypeId.Slider,
                AutomationControlType.Spinner => UiaControlTypeId.Spinner,
                AutomationControlType.StatusBar => UiaControlTypeId.StatusBar,
                AutomationControlType.Tab => UiaControlTypeId.Tab,
                AutomationControlType.TabItem => UiaControlTypeId.TabItem,
                AutomationControlType.Text => UiaControlTypeId.Text,
                AutomationControlType.ToolBar => UiaControlTypeId.ToolBar,
                AutomationControlType.ToolTip => UiaControlTypeId.ToolTip,
                AutomationControlType.Tree => UiaControlTypeId.Tree,
                AutomationControlType.TreeItem => UiaControlTypeId.TreeItem,
                AutomationControlType.Custom => UiaControlTypeId.Custom,
                AutomationControlType.Group => UiaControlTypeId.Group,
                AutomationControlType.Thumb => UiaControlTypeId.Thumb,
                AutomationControlType.DataGrid => UiaControlTypeId.DataGrid,
                AutomationControlType.DataItem => UiaControlTypeId.DataItem,
                AutomationControlType.Document => UiaControlTypeId.Document,
                AutomationControlType.SplitButton => UiaControlTypeId.SplitButton,
                AutomationControlType.Window => UiaControlTypeId.Window,
                AutomationControlType.Pane => UiaControlTypeId.Pane,
                AutomationControlType.Header => UiaControlTypeId.Header,
                AutomationControlType.HeaderItem => UiaControlTypeId.HeaderItem,
                AutomationControlType.Table => UiaControlTypeId.Table,
                AutomationControlType.TitleBar => UiaControlTypeId.TitleBar,
                AutomationControlType.Separator => UiaControlTypeId.Separator,
                _ => UiaControlTypeId.Custom,
            };
        }
    }
}

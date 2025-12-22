using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Threading;
using Avalonia.Win32.Automation.Interop;
using AAP = Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Automation.Interop;

namespace Avalonia.Win32.Automation
{
#if NET8_0_OR_GREATER
    [GeneratedComClass]
    internal partial class AutomationNode :
#else
#if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Requires .NET COM interop")]
#endif
    internal partial class AutomationNode : MarshalByRefObject,
#endif
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
            { AutomationElementIdentifiers.HelpTextProperty, UiaPropertyId.HelpText },
            { AutomationElementIdentifiers.LandmarkTypeProperty, UiaPropertyId.LandmarkType },
            { AutomationElementIdentifiers.HeadingLevelProperty, UiaPropertyId.HeadingLevel },
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
            {
                SelectionItemPatternIdentifiers.SelectionContainerProperty,
                UiaPropertyId.SelectionItemSelectionContainer
            }
        };

        private static ConditionalWeakTable<AutomationPeer, AutomationNode> s_nodes = new();

        private readonly int[] _runtimeId;

        private static readonly int s_pid = GetProcessId();

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

        public virtual Rect GetBoundingRectangle()
        {
            return InvokeSync(() =>
            {
                if (GetRoot() is RootAutomationNode root)
                    return root.ToScreen(Peer.GetBoundingRectangle());
                return default;
            });
        }

        public virtual IRawElementProviderFragmentRoot? GetFragmentRoot()
        {
            return InvokeSync(() => GetRoot());
        }

        public virtual IRawElementProviderSimple? GetHostRawElementProvider() => null;
        public virtual ProviderOptions GetProviderOptions() => ProviderOptions.ServerSideProvider;

        public virtual object? GetPatternProvider(int patternId)
        {
            AutomationNode? ThisIfPeerImplementsProvider<T>() => Peer.GetProvider<T>() is object ? this : null;

            return (UiaPatternId)patternId switch
            {
                UiaPatternId.ExpandCollapse => ThisIfPeerImplementsProvider<AAP.IExpandCollapseProvider>(),
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
            object? value = (UiaPropertyId)propertyId switch
            {
                UiaPropertyId.AcceleratorKey => InvokeSync(() => Peer.GetAcceleratorKey()),
                UiaPropertyId.AccessKey => InvokeSync(() => Peer.GetAccessKey()),
                UiaPropertyId.AutomationId => InvokeSync(() => Peer.GetAutomationId()),
                UiaPropertyId.ClassName => InvokeSync(() => Peer.GetClassName()),
                UiaPropertyId.ClickablePoint => GetBoundingRectangle() is var rect ?
                    new[] { rect.Center.X, rect.Center.Y } :
                    default,
                UiaPropertyId.ControlType => InvokeSync(() => ToUiaControlType(Peer.GetAutomationControlType())),
                UiaPropertyId.Culture => CultureInfo.CurrentCulture.LCID,
                UiaPropertyId.FrameworkId => "Avalonia",
                UiaPropertyId.HasKeyboardFocus => InvokeSync(() => Peer.HasKeyboardFocus()),
                UiaPropertyId.IsContentElement => InvokeSync(() => Peer.IsContentElement()),
                UiaPropertyId.IsControlElement => InvokeSync(() => Peer.IsControlElement()),
                UiaPropertyId.IsEnabled => InvokeSync(() => Peer.IsEnabled()),
                UiaPropertyId.IsKeyboardFocusable => InvokeSync(() => Peer.IsKeyboardFocusable()),
                UiaPropertyId.IsOffscreen => InvokeSync(() => Peer.IsOffscreen()),
                UiaPropertyId.LocalizedControlType => InvokeSync(() => Peer.GetLocalizedControlType()),
                UiaPropertyId.Name => InvokeSync(() => Peer.GetName()),
                UiaPropertyId.HelpText => InvokeSync(() => Peer.GetHelpText()),
                UiaPropertyId.LandmarkType => InvokeSync(() => ToUiaLandmarkType(Peer.GetLandmarkType())),
                UiaPropertyId.LocalizedLandmarkType => InvokeSync(() => ToUiaLocalizedLandmarkType(Peer.GetLandmarkType())),
                UiaPropertyId.HeadingLevel => InvokeSync(() => ToUiaHeadingLevel(Peer.GetHeadingLevel())),
                UiaPropertyId.ProcessId => s_pid,
                UiaPropertyId.RuntimeId => _runtimeId,
                _ => null,
            };

            if (value?.GetType().IsEnum == true)
            {
                return Convert.ToInt32(value!);
            }

            return value;
        }

        public int[] GetRuntimeId() => _runtimeId;

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

#if NET6_0_OR_GREATER
        [return: NotNullIfNotNull(nameof(peer))]
#endif
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
            if (peer is InteropAutomationPeer interop)
                return new InteropAutomationNode(interop);
            else if (peer.GetProvider<AAP.IRootProvider>() is not null)
                return new RootAutomationNode(peer);
            else 
                return new AutomationNode(peer);
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

        private static UiaLandmarkType? ToUiaLandmarkType(AutomationLandmarkType? landmarkType)
        {
            return landmarkType switch
            {
                AutomationLandmarkType.Banner or
                AutomationLandmarkType.Complementary or
                AutomationLandmarkType.ContentInfo or
                AutomationLandmarkType.Region => UiaLandmarkType.Custom,
                AutomationLandmarkType.Form => UiaLandmarkType.Form,
                AutomationLandmarkType.Main => UiaLandmarkType.Main,
                AutomationLandmarkType.Navigation => UiaLandmarkType.Navigation,
                AutomationLandmarkType.Search => UiaLandmarkType.Search,
                _ => null,
            };
        }

        private static string? ToUiaLocalizedLandmarkType(AutomationLandmarkType? landmarkType)
        {
            return landmarkType switch
            {
                AutomationLandmarkType.Banner => "banner",
                AutomationLandmarkType.Complementary => "complementary",
                AutomationLandmarkType.ContentInfo => "content information",
                AutomationLandmarkType.Region => "region",
                _ => null,
            };
        }

        private static UiaHeadingLevel ToUiaHeadingLevel(int level)
        {
            return level switch
            {
                1 => UiaHeadingLevel.Level1,
                2 => UiaHeadingLevel.Level2,
                3 => UiaHeadingLevel.Level3,
                4 => UiaHeadingLevel.Level4,
                5 => UiaHeadingLevel.Level5,
                6 => UiaHeadingLevel.Level6,
                7 => UiaHeadingLevel.Level7,
                8 => UiaHeadingLevel.Level8,
                9 => UiaHeadingLevel.Level9,
                _ => UiaHeadingLevel.None,
            };
        }

        private static int GetProcessId()
        {
#if NET6_0_OR_GREATER
            return Environment.ProcessId;
#else
            using var proccess = Process.GetCurrentProcess();
            return proccess.Id;
#endif
        }
    }
}

using System;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using global::Avalonia.Styling;
using AvApplication = global::Avalonia.Application;

namespace WinUIEmbedSample
{
    public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
    {
        private int _clicks;

        public MainWindow()
        {
            InitializeComponent();

            App.Lifetime.MainView = new EmbeddedView();
            AvaloniaPanel.Content = App.Lifetime.MainView;

            // Probe the automation peer *before* the panel is loaded — answers the
            // open question of whether GetChildren() works before _root.Prepare().
            AppendDiagnostic("ctor: " + DescribePeer());

            AvaloniaPanel.Loading += (_, _) => AppendDiagnostic("Loading: " + DescribePeer());
            AvaloniaPanel.Loaded += (_, _) =>
            {
                AppendDiagnostic("Loaded: " + DescribePeer());
            };
        }

        private string DescribePeer()
        {
            try
            {
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(AvaloniaPanel);
                if (peer is null)
                    return "peer=null";
                var children = peer.GetChildren();
                var count = children?.Count ?? 0;
                var sb = new StringBuilder();
                sb.Append("peer=").Append(peer.GetType().Name)
                  .Append(" class=").Append(peer.GetClassName())
                  .Append(" childCount=").Append(count);
                if (count > 0)
                {
                    sb.Append(" first=[");
                    var first = children![0];
                    sb.Append(first.GetClassName()).Append('/').Append(first.GetAutomationControlType());
                    sb.Append("] firstChildren=").Append(first.GetChildren()?.Count ?? 0);
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "threw: " + ex.GetType().Name + ": " + ex.Message;
            }
        }

        private void ProbePeerButton_Click(object sender, RoutedEventArgs e)
        {
            AppendDiagnostic("Probe: " + DescribePeer());
        }

        private async void ProbeWorkerButton_Click(object sender, RoutedEventArgs e)
        {
            // Cache the peer on the UI thread (CreatePeerForElement is UI-thread bound).
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(AvaloniaPanel);

            // Walk on a worker thread — mimics how UIA queries (it calls from a separate thread).
            var result = await System.Threading.Tasks.Task.Run(() =>
            {
                var sb = new StringBuilder();
                sb.Append("Worker-thread walk (TID=").Append(Environment.CurrentManagedThreadId).Append("):\n");
                try { WalkPeer(peer, 0, sb, maxDepth: 20); }
                catch (Exception ex) { sb.Append("THREW: ").Append(ex); }
                return sb.ToString();
            });

            AppendDiagnostic(result);
        }

        private void ProbeDeepButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(AvaloniaPanel);
                var sb = new StringBuilder();
                sb.Append("Deep walk:\n");
                WalkPeer(peer, 0, sb, maxDepth: 20);
                AppendDiagnostic(sb.ToString());
            }
            catch (Exception ex)
            {
                AppendDiagnostic("Deep probe threw: " + ex.Message);
            }
        }

        private static void WalkPeer(AutomationPeer peer, int depth, StringBuilder sb, int maxDepth)
        {
            if (peer is null || depth > maxDepth)
                return;

            for (var i = 0; i < depth; i++) sb.Append("  ");
            var className = peer.GetClassName();
            var name = peer.GetName();
            var type = peer.GetAutomationControlType();
            var children = peer.GetChildren();
            sb.Append('[').Append(className).Append(']')
              .Append(" type=").Append(type)
              .Append(" name=\"").Append(name).Append('"')
              .Append(" children=").Append(children?.Count ?? 0)
              .Append('\n');

            if (children is null) return;
            foreach (var child in children)
                WalkPeer(child, depth + 1, sb, maxDepth);
        }

        private void AppendDiagnostic(string line)
        {
            if (DiagnosticsLog is null)
                return;
            DiagnosticsLog.Text += "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + line + "\n";
        }

        private void WinUiButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            WinUiClickCount.Text = $"Clicked {++_clicks} times";
        }

        private void WinUiDragSource_DragStarting(
            Microsoft.UI.Xaml.UIElement sender,
            Microsoft.UI.Xaml.DragStartingEventArgs e)
        {
            e.Data.SetText("Hello from WinUI");
            e.AllowedOperations =
                Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy |
                Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
        }

        private void WinUiSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (WinUiSliderValue is not null)
                WinUiSliderValue.Text = $"Slider: {e.NewValue:F0}";
        }

        private void WinUiThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = WinUiThemeCombo.SelectedIndex;

            if (Content is FrameworkElement root)
            {
                root.RequestedTheme = selected switch
                {
                    0 => ElementTheme.Light,
                    1 => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }

            if (AvApplication.Current is { } avApp)
            {
                avApp.RequestedThemeVariant = selected switch
                {
                    0 => ThemeVariant.Light,
                    1 => ThemeVariant.Dark,
                    _ => ThemeVariant.Default
                };
            }
        }
    }
}

using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class ComplexVirtualizationPage : UserControl
    {
        private DispatcherTimer? _statsTimer;
        private ComplexVirtualizationPageViewModel? _viewModel;

        public ComplexVirtualizationPage()
        {
            InitializeComponent();
            _viewModel = new ComplexVirtualizationPageViewModel();
            DataContext = _viewModel;

            // Set initial virtualization state
            ContentVirtualizationDiagnostics.IsEnabled = _viewModel.EnableVirtualization;

            // Listen to EnableVirtualization changes
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // Update cache stats every 500ms for more responsive feedback
            _statsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _statsTimer.Tick += UpdateCacheStats;
            _statsTimer.Start();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ComplexVirtualizationPageViewModel.EnableVirtualization))
            {
                // Control virtualization globally via diagnostics API
                ContentVirtualizationDiagnostics.IsEnabled = _viewModel!.EnableVirtualization;
            }
        }

        private void UpdateCacheStats(object? sender, EventArgs e)
        {
            var listBox = this.FindControl<ListBox>("MainListBox");
            var statsText = this.FindControl<TextBlock>("CacheStatsText");

            if (listBox != null && statsText != null)
            {
                var stats = ContentVirtualizationDiagnostics.GetPoolStats(listBox);
                if (stats != null && stats.PoolEntries.Any())
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("POOL STATISTICS:");
                    sb.AppendLine();

                    var totalPooled = 0;
                    foreach (var entry in stats.PoolEntries.OrderBy(e => e.RecycleKey.ToString()))
                    {
                        var typeName = entry.RecycleKey.ToString()?.Split('.').LastOrDefault() ?? "Unknown";
                        sb.AppendLine($"{typeName}:");
                        sb.AppendLine($"  Pooled: {entry.PooledCount}");
                        totalPooled += entry.PooledCount;
                    }

                    sb.AppendLine();
                    sb.AppendLine($"Total: {totalPooled} controls");
                    sb.AppendLine($"Types: {stats.PoolEntries.Count}");

                    statsText.Text = sb.ToString();
                }
                else
                {
                    statsText.Text = "Virtualization: OFF\n\nEnable virtualization and scroll to see pooled controls.";
                }
            }
        }
    }
}

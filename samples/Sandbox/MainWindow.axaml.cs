using System;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Sandbox
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _timer;

        public MainWindow()
        {
            InitializeComponent();

            var stateCombo = this.FindControl<ComboBox>("StateCombo")!;
            var slider = this.FindControl<Slider>("ProgressSlider")!;
            var valueLabel = this.FindControl<TextBlock>("ValueLabel")!;
            var animateBtn = this.FindControl<Button>("AnimateBtn")!;
            var resetBtn = this.FindControl<Button>("ResetBtn")!;

            stateCombo.SelectionChanged += (_, _) =>
            {
                TaskbarProgressState = stateCombo.SelectedIndex switch
                {
                    0 => Avalonia.Controls.TaskbarProgressState.None,
                    1 => Avalonia.Controls.TaskbarProgressState.Indeterminate,
                    2 => Avalonia.Controls.TaskbarProgressState.Normal,
                    3 => Avalonia.Controls.TaskbarProgressState.Error,
                    4 => Avalonia.Controls.TaskbarProgressState.Paused,
                    _ => Avalonia.Controls.TaskbarProgressState.None,
                };
            };

            slider.PropertyChanged += (_, e) =>
            {
                if (e.Property == Slider.ValueProperty)
                {
                    var pct = slider.Value / 100.0;
                    TaskbarProgressValue = pct;
                    valueLabel.Text = $"Value: {slider.Value:F0}%";
                }
            };

            animateBtn.Click += (_, _) =>
            {
                _timer?.Stop();
                slider.Value = 0;
                TaskbarProgressState = Avalonia.Controls.TaskbarProgressState.Normal;
                stateCombo.SelectedIndex = 2;

                _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
                _timer.Tick += (_, _) =>
                {
                    slider.Value += 1;
                    if (slider.Value >= 100)
                        _timer.Stop();
                };
                _timer.Start();
            };

            resetBtn.Click += (_, _) =>
            {
                _timer?.Stop();
                slider.Value = 0;
                stateCombo.SelectedIndex = 0;
                TaskbarProgressState = Avalonia.Controls.TaskbarProgressState.None;
                TaskbarProgressValue = 0;
            };
        }
    }
}

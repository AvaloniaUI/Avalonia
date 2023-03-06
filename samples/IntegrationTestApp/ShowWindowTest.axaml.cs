using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace IntegrationTestApp
{
    public class MeasureBorder : Border
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            MeasuredWith = availableSize;
            
            return base.MeasureOverride(availableSize);
        }

        public static readonly StyledProperty<Size> MeasuredWithProperty = AvaloniaProperty.Register<MeasureBorder, Size>(
            nameof(MeasuredWith));

        public Size MeasuredWith
        {
            get => GetValue(MeasuredWithProperty);
            set => SetValue(MeasuredWithProperty, value);
        }
    }
    
    public class ShowWindowTest : Window
    {
        private readonly DispatcherTimer? _timer;
        private readonly TextBox? _orderTextBox;
        private bool _opened;
        
        public ShowWindowTest()
        {
            InitializeComponent();
            DataContext = this;
            PositionChanged += (s, e) =>
            {
                this.GetControl<TextBox>("CurrentPosition").Text = $"{Position}";
                UpdateSummary();
            };

            _orderTextBox = this.GetControl<TextBox>("CurrentOrder");
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _timer.Tick += TimerOnTick;
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            
            _timer.Start();
            
            var scaling = PlatformImpl!.DesktopScaling;
            this.GetControl<TextBox>("CurrentPosition").Text = $"{Position}";
            this.GetControl<TextBox>("CurrentScreenRect").Text = $"{Screens.ScreenFromVisual(this)?.WorkingArea}";
            this.GetControl<TextBox>("CurrentScaling").Text = $"{scaling}";

            if (Owner is not null)
            {
                var ownerRect = this.GetControl<TextBox>("CurrentOwnerRect");
                var owner = (Window)Owner;
                ownerRect.Text = $"{owner.Position}, {PixelSize.FromSize(owner.FrameSize!.Value, scaling)}";
            }

            _opened = true;
            
            UpdateSummary();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _timer?.Stop();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            
            UpdateSummary();
        }


        private void UpdateSummary()
        {
            if(!_opened)
                return;
            
            string s = "";
            
            var scaling = PlatformImpl!.DesktopScaling;

            s += $"clientSize:{this.GetControl<TextBox>("CurrentClientSize").Text}::";
            s += $"frameSize:{this.GetControl<TextBox>("CurrentFrameSize").Text}::";
            s += $"position:{this.GetControl<TextBox>("CurrentPosition").Text}::";
            s += $"screen:{this.GetControl<TextBox>("CurrentScreenRect").Text}::";
            s += $"scaling:{this.GetControl<TextBox>("CurrentScaling").Text}::";
            s += $"windowstate:{(this.GetControl<ComboBox>("CurrentWindowState").SelectedItem as ComboBoxItem).Content}::";
            s += $"order:{this.GetControl<TextBox>("CurrentOrder").Text}::";
            s += $"measured:{this.GetControl<TextBlock>("CurrentMeasuredWithText").Text}::";

            if (Owner is not null)
            {
                s += $"ownerrect:{this.GetControl<TextBox>("CurrentOwnerRect").Text}::";
            }
            
            this.GetControl<TextBlock>("CurrentSummary").Text = s;
        }

        private void TimerOnTick(object? sender, EventArgs e)
        {
            _orderTextBox!.Text = MacOSIntegration.GetOrderedIndex(this).ToString();
            
            UpdateSummary();
        }
    }
}

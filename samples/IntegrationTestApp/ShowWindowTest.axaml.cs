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
        
        public ShowWindowTest()
        {
            InitializeComponent();
            DataContext = this;
            PositionChanged += (s, e) => this.GetControl<TextBox>("CurrentPosition").Text = $"{Position}";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _orderTextBox = this.GetControl<TextBox>("CurrentOrder");
                _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
                _timer.Tick += TimerOnTick;
                _timer.Start();
            }
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
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
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _timer?.Stop();
        }

        private void TimerOnTick(object? sender, EventArgs e)
        {
            _orderTextBox!.Text = MacOSIntegration.GetOrderedIndex(this).ToString();
        }
    }
}

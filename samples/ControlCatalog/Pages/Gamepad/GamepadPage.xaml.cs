using System;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public partial class GamepadPage : UserControl, IObserver<GamepadUpdateArgs>
    {

        /// <summary>
        /// Gamepads StyledProperty definition
        /// </summary>
        public static readonly StyledProperty<AvaloniaList<GamepadUserControl>> GamepadsProperty =
            AvaloniaProperty.Register<GamepadPage, AvaloniaList<GamepadUserControl>>(nameof(Gamepads), new());

        /// <summary>
        /// Gets or sets the Gamepads property.
        /// </summary>
        public AvaloniaList<GamepadUserControl> Gamepads
        {
            get => this.GetValue(GamepadsProperty);
            set => SetValue(GamepadsProperty, value);
        }

        private TextBlock TestingTextBlock { get; set; }
        private ItemsControl GamepadsItemsControl { get; set; }

        public GamepadPage()
        {
            InitializeComponent();
            Loaded += GamepadPage_Loaded;
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(GamepadUpdateArgs value)
        {
            if (Gamepads.Count <= value.Device)
            {
                var control = new GamepadUserControl();
                GamepadsItemsControl.Items.Add(control);
                Gamepads.Add(control);
            }

            Gamepads[value.Device].ReceiveUpdate(value);
            
        }

        private void GamepadPage_Loaded(object? sender, RoutedEventArgs e)
        {
            if (!Design.IsDesignMode)
            {
                TopLevel.GetTopLevel(this)?.GamepadManager?.GamepadStream?.Subscribe(this);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            GamepadsItemsControl = this.Get<ItemsControl>(nameof(GamepadsItemsControl));
        }
    }
}

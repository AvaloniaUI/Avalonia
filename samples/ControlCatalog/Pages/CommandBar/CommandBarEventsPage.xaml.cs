using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using MiniMvvm;

namespace ControlCatalog.Pages
{
    public partial class CommandBarEventsPage : UserControl
    {
        private readonly List<string> _log = new();
        private int _primaryCount = 3;
        private int _secondaryCount = 2;

        public CommandBarEventsPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            DemoBar.Opening += OnOpening;
            DemoBar.Opened += OnOpened;
            DemoBar.Closing += OnClosing;
            DemoBar.Closed += OnClosed;
            DemoBar.PropertyChanged += OnBarPropertyChanged;

            AttachItemHandlers(DemoBar.PrimaryCommands);
            AttachItemHandlers(DemoBar.SecondaryCommands);

            AppendLog("Ready");
            RefreshState();
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            DemoBar.Opening -= OnOpening;
            DemoBar.Opened -= OnOpened;
            DemoBar.Closing -= OnClosing;
            DemoBar.Closed -= OnClosed;
            DemoBar.PropertyChanged -= OnBarPropertyChanged;

            DetachItemHandlers(DemoBar.PrimaryCommands);
            DetachItemHandlers(DemoBar.SecondaryCommands);
        }

        private void OnIsOpenChanged(object? sender, RoutedEventArgs e)
        {
            DemoBar.IsOpen = IsOpenCheck.IsChecked == true;
            RefreshState();
        }

        private void OnAddPrimary(object? sender, RoutedEventArgs e)
        {
            _primaryCount++;

            var button = CreateButton($"Primary {_primaryCount}");
            DemoBar.PrimaryCommands.Add(button);

            AppendLog($"Primary +, {DemoBar.PrimaryCommands.Count}");
            RefreshState();
        }

        private void OnRemovePrimary(object? sender, RoutedEventArgs e)
        {
            RemoveLastCommand(DemoBar.PrimaryCommands, "Primary");
        }

        private void OnAddSecondary(object? sender, RoutedEventArgs e)
        {
            _secondaryCount++;

            var button = CreateButton($"Secondary {_secondaryCount}");
            DemoBar.SecondaryCommands.Add(button);

            AppendLog($"Secondary +, {DemoBar.SecondaryCommands.Count}");
            RefreshState();
        }

        private void OnRemoveSecondary(object? sender, RoutedEventArgs e)
        {
            RemoveLastCommand(DemoBar.SecondaryCommands, "Secondary");
        }

        private void OnClearLog(object? sender, RoutedEventArgs e)
        {
            _log.Clear();
            EventLogText.Text = "Log cleared";
        }

        private void OnOpening(object? sender, RoutedEventArgs e)
        {
            AppendLog("Opening");
            RefreshState();
        }

        private void OnOpened(object? sender, RoutedEventArgs e)
        {
            AppendLog("Opened");
            RefreshState();
        }

        private void OnClosing(object? sender, RoutedEventArgs e)
        {
            AppendLog("Closing");
            RefreshState();
        }

        private void OnClosed(object? sender, RoutedEventArgs e)
        {
            AppendLog("Closed");
            RefreshState();
        }

        private void OnBarPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == CommandBar.IsOpenProperty
                || e.Property == CommandBar.HasSecondaryCommandsProperty
                || e.Property == CommandBar.IsOverflowButtonVisibleProperty)
            {
                RefreshState();
            }
        }

        private void RefreshState()
        {
            StateText.Text =
                $"IsOpen: {DemoBar.IsOpen}\n" +
                $"HasSecondaryCommands: {DemoBar.HasSecondaryCommands}\n" +
                $"IsOverflowButtonVisible: {DemoBar.IsOverflowButtonVisible}\n" +
                $"Primary: {DemoBar.PrimaryCommands.Count}\n" +
                $"Secondary: {DemoBar.SecondaryCommands.Count}\n" +
                $"OverflowItems: {DemoBar.OverflowItems.Count}";

            IsOpenCheck.IsChecked = DemoBar.IsOpen;
        }

        private void OnCommandItemClick(object? sender, RoutedEventArgs e)
        {
            if (sender is CommandBarButton button)
                AppendLog($"Click, {button.Label}, {DescribePlacement(button)}");
        }

        private CommandBarButton CreateButton(string label)
        {
            var button = new CommandBarButton
            {
                Label = label,
                Icon = new PathIcon
                {
                    Data = StreamGeometry.Parse("M19,13H13V19H11V13H5V11H11V5H13V11H19V13Z")
                }
            };

            AttachItemHandler(button);
            return button;
        }

        private void AttachItemHandlers(IEnumerable<ICommandBarElement> items)
        {
            foreach (var item in items)
                AttachItemHandler(item);
        }

        private void DetachItemHandlers(IEnumerable<ICommandBarElement> items)
        {
            foreach (var item in items)
            {
                if (item is CommandBarButton button)
                    button.Click -= OnCommandItemClick;
            }
        }

        private void AttachItemHandler(ICommandBarElement item)
        {
            if (item is not CommandBarButton button)
                return;

            button.Click -= OnCommandItemClick;
            button.Click += OnCommandItemClick;
            button.Command = MiniCommand.Create(() => AppendLog($"Command, {button.Label}, {DescribePlacement(button)}"));
        }

        private void RemoveLastCommand(IList<ICommandBarElement> items, string bucketName)
        {
            if (items.Count == 0)
                return;

            var item = items[^1];
            var label = item is CommandBarButton button ? button.Label ?? "(unnamed)" : item.GetType().Name;

            if (item is CommandBarButton commandBarButton)
                commandBarButton.Click -= OnCommandItemClick;

            items.RemoveAt(items.Count - 1);

            AppendLog($"{bucketName} -, {label}, {items.Count}");
            RefreshState();
        }

        private static string DescribePlacement(CommandBarButton button)
        {
            return button.IsInOverflow ? "overflow" : "primary";
        }

        private void AppendLog(string message)
        {
            _log.Add(message);

            if (_log.Count > 12)
                _log.RemoveAt(0);

            EventLogText.Text = string.Join("\n", _log.Select((entry, index) => $"{index + 1,2}. {entry}"));
        }
    }
}

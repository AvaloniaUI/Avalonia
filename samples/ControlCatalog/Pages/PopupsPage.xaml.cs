using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages;

public class PopupsPage : UserControl
{
    private Popup? _popup;
    private Popup? _topMostPopup;
    public PopupsPage()
    {
        InitializeComponent();

    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void ButtonLightDismiss_OnClick(object sender, RoutedEventArgs e)
    {
        new Popup
        {
            Placement = PlacementMode.Bottom,
            PlacementTarget = sender as Button,
            Child = new Panel { Margin = new Thickness(10) ,Children = { new TextBlock { Text = "Popup Content" } }},
            IsLightDismissEnabled = true,
            IsOpen = true
        };
    }

    private void ButtonPopupStaysOpen_OnClick(object sender, RoutedEventArgs e)
    {
        if (_popup is not null)
            return;
        
        var closeButton = new Button { Content = "Click to Close" };
        closeButton.Click += (o, args) =>
        {
            if (_popup is null)
                return;
            _popup.IsOpen = false;
            _popup = null;
        };
        _popup = new Popup
        {
            Placement = PlacementMode.Bottom,
            PlacementTarget = sender as Button,
            Child = new Panel
            {
                Margin = new Thickness(10), Children = { closeButton }
            },
            IsLightDismissEnabled = false,
            IsOpen = true
        };
    }

    private void ButtonTopMostPopupStaysOpen(object sender, RoutedEventArgs e)
    {
        if (_topMostPopup is not null)
            return;
        
        var closeButton = new Button { Content = "Click to Close" };
        closeButton.Click += (o, args) =>
        {
            if (_topMostPopup is null)
                return;
            _topMostPopup.IsOpen = false;
            _topMostPopup = null;
        };
        _topMostPopup = new Popup
        {
            Placement = PlacementMode.Bottom,
            PlacementTarget = sender as Button,
            Child = new Panel
            {
                Margin = new Thickness(10), Children = { closeButton }
            },
            IsLightDismissEnabled = false,
            Topmost = true,
            IsOpen = true
        };
    }
}

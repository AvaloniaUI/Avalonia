using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages;

public partial class RetroGamingSearchView : UserControl
{
    public Action? CloseRequested  { get; set; }
    public Action<string>? GameSelected { get; set; }

    public RetroGamingSearchView() => InitializeComponent();

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        CloseBtn.Click               += (_, _) => CloseRequested?.Invoke();
        SearchCyberNinjaBtn.Click    += (_, _) => GameSelected?.Invoke("Cyber Ninja 2084");
        SearchNeonRacerBtn.Click     += (_, _) => GameSelected?.Invoke("Neon Racer");
        SearchDungeonBitBtn.Click    += (_, _) => GameSelected?.Invoke("Dungeon Bit");
        SearchForestSpiritBtn.Click  += (_, _) => GameSelected?.Invoke("Forest Spirit");
        SearchPixelQuestBtn.Click    += (_, _) => GameSelected?.Invoke("Pixel Quest");
        SearchSpaceVoidsBtn.Click    += (_, _) => GameSelected?.Invoke("Space Voids");
        SearchCyberCityBtn.Click     += (_, _) => GameSelected?.Invoke("Cyber City");
    }
}

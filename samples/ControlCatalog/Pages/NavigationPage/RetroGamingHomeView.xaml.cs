using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages;

public partial class RetroGamingHomeView : UserControl
{
    public Action<string>? GameSelected { get; set; }

    public RetroGamingHomeView() => InitializeComponent();

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        HeroPlayBtn.Click              += (_, _) => GameSelected?.Invoke("Cyber Ninja 2084");
        ContinuePixelQuestBtn.Click    += (_, _) => GameSelected?.Invoke("Pixel Quest");
        ContinueSpaceVoidsBtn.Click    += (_, _) => GameSelected?.Invoke("Space Voids");
        NewReleaseNeonRacerBtn.Click   += (_, _) => GameSelected?.Invoke("Neon Racer");
        NewReleaseDungeonBitBtn.Click  += (_, _) => GameSelected?.Invoke("Dungeon Bit");
        NewReleaseForestSpiritBtn.Click += (_, _) => GameSelected?.Invoke("Forest Spirit");
        NewReleaseCyberCityBtn.Click   += (_, _) => GameSelected?.Invoke("Cyber City");
    }
}

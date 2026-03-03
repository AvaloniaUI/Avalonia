using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace ControlCatalog.Pages;

public partial class RetroGamingGamesView : UserControl
{
    public Action<string>? GameSelected { get; set; }

    public RetroGamingGamesView() => InitializeComponent();

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        GameCyberNinjaBtn.Click   += (_, _) => GameSelected?.Invoke("Cyber Ninja 2084");
        GameNeonRacerBtn.Click    += (_, _) => GameSelected?.Invoke("Neon Racer");
        GameDungeonBitBtn.Click   += (_, _) => GameSelected?.Invoke("Dungeon Bit");
        GameForestSpiritBtn.Click += (_, _) => GameSelected?.Invoke("Forest Spirit");
        GamePixelQuestBtn.Click   += (_, _) => GameSelected?.Invoke("Pixel Quest");
        GameSpaceVoidsBtn.Click   += (_, _) => GameSelected?.Invoke("Space Voids");
        GameCyberCityBtn.Click    += (_, _) => GameSelected?.Invoke("Cyber City");

        GamesGrid.SizeChanged += OnGridSizeChanged;
    }

    void OnGridSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        const double defaultWidth = 145;
        var available = GamesGrid.Bounds.Width;
        if (available <= 0) return;

        bool singleColumn = available < defaultWidth * 2;
        foreach (var child in GamesGrid.Children)
        {
            if (child is Button btn && btn.Content is Border card)
                card.Width = singleColumn ? available : defaultWidth;
        }
    }
}

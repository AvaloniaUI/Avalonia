using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace ControlCatalog.Pages;

public partial class RetroGamingDetailView : UserControl
{
    static readonly Dictionary<string, string> GameAssets = new()
    {
        { "Cyber Ninja 2084", "hero.jpg"          },
        { "Pixel Quest",      "pixel_quest.jpg"   },
        { "Neon Racer",       "neon_racer.jpg"    },
        { "Dungeon Bit",      "dungeon_bit.jpg"   },
        { "Forest Spirit",    "forest_spirit.jpg" },
        { "Cyber City",       "cyber_city.jpg"    },
        { "Neon Ninja",       "neon_ninja.jpg"    },
        { "Space Voids",      "space_voids.jpg"   },
    };

    public RetroGamingDetailView() => InitializeComponent();

    public RetroGamingDetailView(string gameTitle)
    {
        InitializeComponent();

        DetailTitleText.Text = gameTitle.ToUpperInvariant();

        var filename = GameAssets.TryGetValue(gameTitle, out var f) ? f
            : (GameAssets.TryGetValue("Neon Ninja", out var fb) ? fb : null);

        if (filename != null)
        {
            try
            {
                var uri = new Uri($"avares://ControlCatalog/Assets/RetroGaming/{filename}");
                using var stream = AssetLoader.Open(uri);
                var bmp = new Bitmap(stream);
                DetailHeroImageBorder.Background = new ImageBrush(bmp)
                {
                    Stretch = Stretch.UniformToFill,
                };
            }
            catch
            {
                SetFallbackBackground();
            }
        }
        else
        {
            SetFallbackBackground();
        }
    }

    void SetFallbackBackground()
    {
        var grad = new LinearGradientBrush
        {
            StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
            EndPoint   = new Avalonia.RelativePoint(1, 1, Avalonia.RelativeUnit.Relative),
        };
        grad.GradientStops.Add(new GradientStop(Avalonia.Media.Color.Parse("#3d2060"), 0));
        grad.GradientStops.Add(new GradientStop(Avalonia.Media.Color.Parse("#120a1f"), 1));
        DetailHeroImageBorder.Background = grad;
    }
}

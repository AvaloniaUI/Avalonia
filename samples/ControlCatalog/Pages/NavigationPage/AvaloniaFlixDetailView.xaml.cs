using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace ControlCatalog.Pages;

public partial class AvaloniaFlixDetailView : UserControl
{
    static readonly string[] MovieAssets =
    {
        "avares://ControlCatalog/Assets/Movies/trending1.jpg",
        "avares://ControlCatalog/Assets/Movies/trending2.jpg",
        "avares://ControlCatalog/Assets/Movies/toprated1.jpg",
        "avares://ControlCatalog/Assets/Movies/toprated2.jpg",
        "avares://ControlCatalog/Assets/Movies/toprated3.jpg",
        "avares://ControlCatalog/Assets/Movies/toprated4.jpg",
        "avares://ControlCatalog/Assets/Movies/continue1.jpg",
        "avares://ControlCatalog/Assets/Movies/morelike1.jpg",
        "avares://ControlCatalog/Assets/Movies/search1.jpg",
        "avares://ControlCatalog/Assets/Movies/hero.jpg",
        "avares://ControlCatalog/Assets/Movies/cast1.jpg",
        "avares://ControlCatalog/Assets/Movies/cast2.jpg",
    };

    public AvaloniaFlixDetailView() => InitializeComponent();

    public AvaloniaFlixDetailView(string movieTitle)
    {
        InitializeComponent();

        HeroTitleLabel.Text = movieTitle;

        var rng    = new Random(movieTitle.GetHashCode());
        int imgIdx = Math.Abs(movieTitle.GetHashCode()) % MovieAssets.Length;

        string year     = (2020 + rng.Next(6)).ToString();
        string rating   = $"{6.5 + rng.NextDouble() * 3.0:F1}/10";
        int    mins     = 90 + rng.Next(60);
        string duration = $"{mins / 60}h {mins % 60}m";

        YearLabel.Text     = year;
        RatingLabel.Text   = rating;
        DurationLabel.Text = duration;

        try
        {
            var uri = new Uri(MovieAssets[imgIdx]);
            HeroBg.Background = new ImageBrush(new Bitmap(AssetLoader.Open(uri)))
            {
                Stretch = Stretch.UniformToFill,
            };
        }
        catch
        {
            HeroBg.Background = new SolidColorBrush(Color.Parse("#111111"));
        }
    }
}

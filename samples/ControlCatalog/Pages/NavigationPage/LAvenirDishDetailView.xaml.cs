using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace ControlCatalog.Pages;

public partial class LAvenirDishDetailView : UserControl
{
    public LAvenirDishDetailView() => InitializeComponent();

    public LAvenirDishDetailView(string name, string price, string description, string imageFile)
    {
        InitializeComponent();

        TitleLabel.Text = name;
        PriceLabel.Text = price;
        DescriptionLabel.Text = description;

        try
        {
            var uri = new Uri($"avares://ControlCatalog/Assets/Restaurant/{imageFile}");
            HeroBg.Background = new ImageBrush(new Bitmap(AssetLoader.Open(uri)))
            {
                Stretch = Stretch.UniformToFill
            };
        }
        catch
        {
            HeroBg.Background = new SolidColorBrush(Color.Parse("#1a1836"));
        }
    }
}

using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ControlCatalog.Models;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class SectionPage : ContentPage
    {
        public SectionPage(HomeSection homeSection)
        {
            InitializeComponent();

            DataContext = new SectionViewModel(homeSection);
        }

        public SectionPage()
        {
            InitializeComponent();
        }
    }
}

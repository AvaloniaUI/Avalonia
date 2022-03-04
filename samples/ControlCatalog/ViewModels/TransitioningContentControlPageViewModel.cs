using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class TransitioningContentControlPageViewModel : ViewModelBase
    {
        public TransitioningContentControlPageViewModel()
        {
            var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>();

            var images = new string[] 
            { 
                "delicate-arch-896885_640.jpg", 
                "hirsch-899118_640.jpg", 
                "maple-leaf-888807_640.jpg" 
            };

            foreach (var image in images)
            {
                var path = $"avares://ControlCatalog/Assets/{image}";
                Images.Add(new Bitmap(assetLoader.Open(new Uri(path))));
            }

            SelectedImage = Images[0];
            SelectedTransition = PageTransitions[0];
        }

        public List<PageTransition> PageTransitions { get; } = new List<PageTransition>()
        {
            new PageTransition("Slide horizontally", new PageSlide(TimeSpan.FromMilliseconds(500), PageSlide.SlideAxis.Horizontal)),
            new PageTransition("Slide vertically", new PageSlide(TimeSpan.FromMilliseconds(500), PageSlide.SlideAxis.Vertical))
        };

        public List<Bitmap> Images { get; } = new List<Bitmap>();


        private Bitmap? _SelectedImage;

        /// <summary>
        /// Gets or Sets the selected image
        /// </summary>
        public Bitmap? SelectedImage
        {
            get { return _SelectedImage; }
            set { this.RaiseAndSetIfChanged(ref _SelectedImage, value); }
        }


        private PageTransition _SelectedTransition;

        /// <summary>
        /// Gets or sets the transition to play
        /// </summary>
        public PageTransition SelectedTransition
        {
            get { return _SelectedTransition; }
            set { this.RaiseAndSetIfChanged(ref _SelectedTransition, value); }
        }

        public void NextImage()
        {
            var index = Images.IndexOf(SelectedImage) + 1;
            
            if (index >= Images.Count)
            {
                index = 0;
            }

            SelectedImage = Images[index];
        }

        public void PrevImage()
        {
            var index = Images.IndexOf(SelectedImage) - 1;

            if (index < 0)
            {
                index = Images.Count-1;
            }

            SelectedImage = Images[index];
        }
    }

    public class PageTransition
    {
        public PageTransition(string displayTitle, IPageTransition transition)
        {
            DisplayTitle = displayTitle;
            Transition = transition;
        }

        public string DisplayTitle { get; }
        public IPageTransition Transition { get; }

        public override string ToString()
        {
            return DisplayTitle;
        }

    }
}

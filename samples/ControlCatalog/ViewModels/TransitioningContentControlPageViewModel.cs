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
            SelectedTransition = PageTransitions[1];
        }

        public List<PageTransition> PageTransitions { get; } = new List<PageTransition>()
        {
            new PageTransition("None", null),
            new PageTransition("CrossFade", new CrossFade(TimeSpan.FromMilliseconds(500))),
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



        private bool _ClipToBounds;

        /// <summary>
        /// Gets or sets if the content should be clipped to bounds
        /// </summary>
        public bool ClipToBounds
        {
            get { return _ClipToBounds; }
            set { this.RaiseAndSetIfChanged(ref _ClipToBounds, value); }
        }


        private int _Duration = 500;

        /// <summary>
        /// Gets or Sets the duration
        /// </summary>
        public int Duration 
        {
            get { return _Duration; }
            set 
            { 
                this.RaiseAndSetIfChanged(ref _Duration , value);

                PageTransitions[1].Transition = new CrossFade(TimeSpan.FromMilliseconds(value));
                PageTransitions[2].Transition = new PageSlide(TimeSpan.FromMilliseconds(value), PageSlide.SlideAxis.Horizontal);
                PageTransitions[3].Transition = new PageSlide(TimeSpan.FromMilliseconds(value), PageSlide.SlideAxis.Vertical);
            }
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

    public class PageTransition : ViewModelBase
    {
        public PageTransition(string displayTitle, IPageTransition transition)
        {
            DisplayTitle = displayTitle;
            Transition = transition;
        }

        public string DisplayTitle { get; }


        private IPageTransition _Transition;

        /// <summary>
        /// Gets or sets the transition
        /// </summary>
        public IPageTransition Transition
        {
            get { return _Transition; }
            set { this.RaiseAndSetIfChanged(ref _Transition, value); }
        }

        public override string ToString()
        {
            return DisplayTitle;
        }

    }
}

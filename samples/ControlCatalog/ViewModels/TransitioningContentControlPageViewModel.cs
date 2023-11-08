using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.VisualTree;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class TransitioningContentControlPageViewModel : ViewModelBase
    {
        public TransitioningContentControlPageViewModel()
        {
            var assetLoader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();

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

            SetupTransitions();

            _SelectedTransition = PageTransitions[1];
            _SelectedImage = Images[0];
        }

        public List<PageTransition> PageTransitions { get; } = new List<PageTransition>();

        public List<Bitmap> Images { get; } = new List<Bitmap>();


        private Bitmap _SelectedImage;
        private bool _Reversed;

        /// <summary>
        /// Gets or Sets the selected image
        /// </summary>
        public Bitmap SelectedImage
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
                SetupTransitions();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the animation is reversed.
        /// </summary>
        public bool Reversed
        {
            get => _Reversed;
            set => this.RaiseAndSetIfChanged(ref _Reversed, value);
        }

        private void SetupTransitions()
        {
            if (PageTransitions.Count == 0)
            {
                PageTransitions.AddRange(new[] 
                {
                    new PageTransition("None"),
                    new PageTransition("CrossFade"),
                    new PageTransition("Slide horizontally"),
                    new PageTransition("Slide vertically"),
                    new PageTransition("Composite"),
                    new PageTransition("Custom")
                });
            }

            PageTransitions[1].Transition = new CrossFade(TimeSpan.FromMilliseconds(Duration));
            PageTransitions[2].Transition = new PageSlide(TimeSpan.FromMilliseconds(Duration), PageSlide.SlideAxis.Horizontal);
            PageTransitions[3].Transition = new PageSlide(TimeSpan.FromMilliseconds(Duration), PageSlide.SlideAxis.Vertical);

            var compositeTransition = new CompositePageTransition();
            compositeTransition.PageTransitions.Add(PageTransitions[1].Transition!);
            compositeTransition.PageTransitions.Add(PageTransitions[2].Transition!);
            compositeTransition.PageTransitions.Add(PageTransitions[3].Transition!);
            PageTransitions[4].Transition = compositeTransition;

            PageTransitions[5].Transition = new CustomTransition(TimeSpan.FromMilliseconds(Duration));
        }

        public void NextImage()
        {
            Reversed = false;
            var index = Images.IndexOf(SelectedImage) + 1;
            
            if (index >= Images.Count)
            {
                index = 0;
            }

            SelectedImage = Images[index];
        }

        public void PrevImage()
        {
            Reversed = true;
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
        public PageTransition(string displayTitle)
        {
            DisplayTitle = displayTitle;
        }

        public string DisplayTitle { get; }


        private IPageTransition? _Transition;

        /// <summary>
        /// Gets or sets the transition
        /// </summary>
        public IPageTransition? Transition
        {
            get { return _Transition; }
            set { this.RaiseAndSetIfChanged(ref _Transition, value); }
        }

        public override string ToString()
        {
            return DisplayTitle;
        }

    }

    public class CustomTransition : IPageTransition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTransition"/> class.
        /// </summary>
        public CustomTransition()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTransition"/> class.
        /// </summary>
        /// <param name="duration">The duration of the animation.</param>
        public CustomTransition(TimeSpan duration)
        {
            Duration = duration;
        }

        /// <summary>
        /// Gets the duration of the animation.
        /// </summary>
        public TimeSpan Duration { get; set; }

        public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var tasks = new List<Task>();
            var parent = GetVisualParent(from, to);
            var scaleProperty = ScaleTransform.ScaleYProperty;

            if (from != null)
            {
                var animation = new Animation
                {
                    Children =
                    {
                        new KeyFrame
                        {
                            Setters = { new Setter { Property = scaleProperty, Value = 1d } },
                            Cue = new Cue(0d)
                        },
                        new KeyFrame
                        {
                            Setters =
                            {
                                new Setter
                                {
                                    Property = scaleProperty,
                                    Value = 0d
                                }
                            },
                            Cue = new Cue(1d)
                        }
                    },
                    Duration = Duration
                };
                tasks.Add(animation.RunAsync(from, null, cancellationToken));
            }

            if (to != null)
            {
                to.IsVisible = true;
                var animation = new Animation
                {
                    Children =
                    {
                        new KeyFrame
                        {
                            Setters =
                            {
                                new Setter
                                {
                                    Property = scaleProperty,
                                    Value = 0d
                                }
                            },
                            Cue = new Cue(0d)
                        },
                        new KeyFrame
                        {
                            Setters = { new Setter { Property = scaleProperty, Value = 1d } },
                            Cue = new Cue(1d)
                        }
                    },
                    Duration = Duration
                };
                tasks.Add(animation.RunAsync(to, null, cancellationToken));
            }

            await Task.WhenAll(tasks);

            if (from != null && !cancellationToken.IsCancellationRequested)
            {
                from.IsVisible = false;
            }
        }

        /// <summary>
        /// Gets the common visual parent of the two control.
        /// </summary>
        /// <param name="from">The from control.</param>
        /// <param name="to">The to control.</param>
        /// <returns>The common parent.</returns>
        /// <exception cref="ArgumentException">
        /// The two controls do not share a common parent.
        /// </exception>
        /// <remarks>
        /// Any one of the parameters may be null, but not both.
        /// </remarks>
        private static Visual GetVisualParent(Visual? from, Visual? to)
        {
            var p1 = (from ?? to)!.GetVisualParent();
            var p2 = (to ?? from)!.GetVisualParent();

            if (p1 != null && p2 != null && p1 != p2)
            {
                throw new ArgumentException("Controls for PageSlide must have same parent.");
            }

            return p1 ?? throw new InvalidOperationException("Cannot determine visual parent.");
        }
    }
}

using System;
using System.Diagnostics;
using Avalonia.Controls.Metadata;
using Avalonia.Logging;

namespace Avalonia.Controls
{
    [PseudoClasses(pcVisited)]
    public class HyperlinkButton: Button
    {
        public const string pcVisited = ":visited";
        
        /// <summary>
        /// Defines the <see cref="NavigateUri"/> property.
        /// </summary>
        public static readonly DirectProperty<HyperlinkButton, Uri?> NavigateUriProperty =
            AvaloniaProperty.RegisterDirect<HyperlinkButton, Uri?>(nameof(NavigateUri), x => x.NavigateUri, (x, v) => x.NavigateUri = v);

        private Uri? _navigateUri;
        
        /// <summary>
        /// Gets or sets the Uri to navigate to when the button is clicked.
        /// </summary>
        public Uri? NavigateUri
        {
            get => _navigateUri;
            set => SetAndRaise(NavigateUriProperty, ref _navigateUri, value);
        }
        
        /// <summary>
        /// Defines the <see cref="IsVisited"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsVisitedProperty =
            AvaloniaProperty.Register<HyperlinkButton, bool>(nameof(IsVisited));
        
        /// <summary>
        /// Gets or sets if the <see cref="NavigateUri"/> is visited for this button.
        /// </summary>
        public bool IsVisited
        {
            get => GetValue(IsVisitedProperty);
            set => SetValue(IsVisitedProperty, value);
        }

        /// <summary>
        /// Defines the <see cref="IsVisitedSetOnClick"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsVisitedSetOnClickProperty =
            AvaloniaProperty.Register<HyperlinkButton, bool>(nameof(IsVisitedSetOnClick), true);
        
        /// <summary>
        /// <para>Gets or Sets whether <see cref="IsVisited"/> property should be determined when button is clicked. </para>
        /// <para>If this property is set to true, then <see cref="IsVisited"/> is automatically set to true when button is clicked. </para>
        /// <para>If this property is set to false, then <see cref="IsVisited"/> takes assigned value. </para>
        /// </summary>
        public bool IsVisitedSetOnClick
        {
            get => GetValue(IsVisitedSetOnClickProperty);
            set => SetValue(IsVisitedSetOnClickProperty, value);
        }

        protected override void OnClick()
        {
            base.OnClick();
            if (NavigateUri is not null)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(NavigateUri.ToString()) { UseShellExecute = true });
                    if (IsVisitedSetOnClick)
                    {
                        IsVisited = true;
                    }
                }
                catch
                {
                    Logger.TryGet(LogEventLevel.Error, $"Unable to open Uri {NavigateUri}");
                }
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == HyperlinkButton.IsVisitedProperty)
            {
                bool newValue = change.GetNewValue<bool>();
                PseudoClasses.Set(pcVisited, newValue);
            }
        }
    }
}

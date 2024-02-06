using System;
using Avalonia.Controls.Metadata;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    /// <summary>
    /// A button control that functions as a navigateable hyperlink.
    /// </summary>
    [PseudoClasses(pcVisited)]
    public class HyperlinkButton : Button
    {
        // See: https://www.w3schools.com/cssref/sel_visited.php
        private const string pcVisited = ":visited";

        /// <summary>
        /// Defines the <see cref="IsVisited"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsVisitedProperty =
            AvaloniaProperty.Register<HyperlinkButton, bool>(
                nameof(IsVisited),
                defaultValue: false);

        /// <summary>
        /// Defines the <see cref="IsVisitedOnClick"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsVisitedOnClickProperty =
            AvaloniaProperty.Register<HyperlinkButton, bool>(
                nameof(IsVisitedOnClick),
                defaultValue: true);

        /// <summary>
        /// Defines the <see cref="NavigateUri"/> property.
        /// </summary>
        public static readonly StyledProperty<Uri?> NavigateUriProperty =
            AvaloniaProperty.Register<HyperlinkButton, Uri?>(
                nameof(NavigateUri),
                defaultValue: null);

        /// <summary>
        /// Initializes a new instance of the <see cref="HyperlinkButton"/> class.
        /// </summary>
        public HyperlinkButton()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="NavigateUri"/> has been visited.
        /// </summary>
        public bool IsVisited
        {
            get => GetValue(IsVisitedProperty);
            set => SetValue(IsVisitedProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="IsVisited"/> property is automatically
        /// set to true when the <see cref="HyperlinkButton"/> is clicked and the <see cref="NavigateUri"/>
        /// is successfully launched.
        /// </summary>
        public bool IsVisitedOnClick
        {
            get => GetValue(IsVisitedOnClickProperty);
            set => SetValue(IsVisitedOnClickProperty, value);
        }

        /// <summary>
        /// Gets or sets the Uniform Resource Identifier (URI) navigated to when clicked.
        /// </summary>
        /// <remarks>
        /// The URI may be any website or file location that can be launched by the Launcher service.
        /// </remarks>
        public Uri? NavigateUri
        {
            get => GetValue(NavigateUriProperty);
            set => SetValue(NavigateUriProperty, value);
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == HyperlinkButton.IsVisitedProperty)
            {
                PseudoClasses.Set(pcVisited, change.GetNewValue<bool>());
            }
        }

        /// <inheritdoc/>
        protected override void OnClick()
        {
            base.OnClick();

            if (IsVisitedOnClick)
            {
                Uri? uri = NavigateUri;

                if (uri is not null)
                {
                    Dispatcher.UIThread.Post(async () =>
                    {
                        bool success = true; // await TopLevel.GetTopLevel(this)!.Launcher.LaunchUriAsync(uri);
                        if (success)
                        {
                            SetCurrentValue(IsVisitedProperty, true);
                        }
                    });
                }
            }
        }
    }
}

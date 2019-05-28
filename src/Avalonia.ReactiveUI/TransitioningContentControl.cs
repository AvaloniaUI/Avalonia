// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Avalonia.ReactiveUI
{
    /// <summary>
    /// A ContentControl that animates the transition when its content is changed.
    /// </summary>
    public class TransitioningContentControl : ContentControl, IStyleable
    {
        /// <summary>
        /// <see cref="AvaloniaProperty"/> for the <see cref="FadeInAnimation"/> property.
        /// </summary>
        public static readonly AvaloniaProperty<IAnimation> FadeInAnimationProperty =
            AvaloniaProperty.Register<TransitioningContentControl, IAnimation>(nameof(DefaultContent),
                CreateOpacityAnimation(0d, 1d, TimeSpan.FromSeconds(0.25)));

        /// <summary>
        /// <see cref="AvaloniaProperty"/> for the <see cref="FadeOutAnimation"/> property.
        /// </summary>
        public static readonly AvaloniaProperty<IAnimation> FadeOutAnimationProperty =
            AvaloniaProperty.Register<TransitioningContentControl, IAnimation>(nameof(DefaultContent),
                CreateOpacityAnimation(1d, 0d, TimeSpan.FromSeconds(0.25)));

        /// <summary>
        /// <see cref="AvaloniaProperty"/> for the <see cref="DefaultContent"/> property.
        /// </summary>
        public static readonly AvaloniaProperty<object> DefaultContentProperty =
            AvaloniaProperty.Register<TransitioningContentControl, object>(nameof(DefaultContent));
        
        /// <summary>
        /// Gets or sets the animation played when content appears.
        /// </summary>
        public IAnimation FadeInAnimation
        {
            get => GetValue(FadeInAnimationProperty);
            set => SetValue(FadeInAnimationProperty, value);
        }

        /// <summary>
        /// Gets or sets the animation played when content disappears.
        /// </summary>
        public IAnimation FadeOutAnimation
        {
            get => GetValue(FadeOutAnimationProperty);
            set => SetValue(FadeOutAnimationProperty, value);
        }
        
        /// <summary>
        /// Gets or sets the content displayed whenever there is no page currently routed.
        /// </summary>
        public object DefaultContent
        {
            get => GetValue(DefaultContentProperty);
            set => SetValue(DefaultContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the content with animation.
        /// </summary>
        public new object Content
        {
            get => base.Content;
            set => UpdateContentWithTransition(value);
        }
        
        /// <summary>
        /// TransitioningContentControl uses the default ContentControl 
        /// template from Avalonia default theme.
        /// </summary>
        Type IStyleable.StyleKey => typeof(ContentControl);

        /// <summary>
        /// Updates the content with transitions.
        /// </summary>
        /// <param name="content">New content to set.</param>
        private async void UpdateContentWithTransition(object content)
        {
            if (FadeOutAnimation != null)
                await FadeOutAnimation.RunAsync(this, Clock);
            base.Content = content;
            if (FadeInAnimation != null)
                await FadeInAnimation.RunAsync(this, Clock);
        }
        
        /// <summary>
        /// Creates opacity animation for this routed view host.
        /// </summary>
        /// <param name="from">Opacity to start from.</param>
        /// <param name="to">Opacity to finish with.</param>
        /// <param name="duration">Duration of the animation.</param>
        /// <returns>Animation object instance.</returns>
        private static IAnimation CreateOpacityAnimation(double from, double to, TimeSpan duration) 
        {
            return new Avalonia.Animation.Animation
            {
                Duration = duration,
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = OpacityProperty,
                                Value = from
                            }
                        },
                        Cue = new Cue(0d)
                    },
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = OpacityProperty,
                                Value = to
                            }
                        },
                        Cue = new Cue(1d)
                    }
                }
            };
        }
    }
}
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Animation.Easings;
using Avalonia.Controls;

namespace Avalonia.Animation
{
    /// <summary>
    /// Coordinates connected animations across views.
    /// Each <see cref="TopLevel"/> window has its own independent instance so
    /// animations cannot bleed across windows.
    /// </summary>
    /// <remarks>
    /// Typical usage:
    /// <list type="number">
    ///   <item>On the source view, call <see cref="PrepareToAnimate"/> to capture the element.</item>
    ///   <item>Navigate to the destination view.</item>
    ///   <item>On the destination view, call <see cref="GetAnimation"/> then
    ///   <c>TryStart</c> on the returned animation to run the animation.</item>
    /// </list>
    /// </remarks>
    internal class ConnectedAnimationService : AvaloniaObject
    {
        private static readonly ConditionalWeakTable<TopLevel, ConnectedAnimationService> s_perView = new();
        private readonly Dictionary<string, ConnectedAnimation> _animations = new();

        internal ConnectedAnimationService()
        {
            DefaultDuration = TimeSpan.FromMilliseconds(300);
        }

        /// <summary>
        /// Gets the <see cref="ConnectedAnimationService"/> for the specified <paramref name="topLevel"/>.
        /// Each top-level window has its own isolated instance.
        /// </summary>
        public static ConnectedAnimationService GetForTopLevel(TopLevel topLevel)
        {
            ArgumentNullException.ThrowIfNull(topLevel);
            return s_perView.GetValue(topLevel, static _ => new ConnectedAnimationService());
        }

        /// <summary>
        /// Gets or sets the default duration applied to all animations whose
        /// configuration does not specify one.  Defaults to 300 ms.
        /// </summary>
        public TimeSpan DefaultDuration { get; set; }

        /// <summary>
        /// Gets or sets the default easing function applied when the active
        /// <see cref="ConnectedAnimationConfiguration"/> does not specify one.
        /// When <see langword="null"/> a configuration-specific default is used.
        /// </summary>
        public Easing? DefaultEasingFunction { get; set; }

        /// <summary>
        /// Captures <paramref name="source"/> and registers a pending animation under
        /// <paramref name="key"/>.  Call this on the source view <em>before</em> navigating away.
        /// </summary>
        /// <param name="key">Unique string that pairs this call with the matching
        /// <see cref="GetAnimation"/> call on the destination view.</param>
        /// <param name="source">The element to animate from.</param>
        /// <returns>The prepared <see cref="ConnectedAnimation"/>.</returns>
        public ConnectedAnimation PrepareToAnimate(string key, Visual source)
        {
            ArgumentException.ThrowIfNullOrEmpty(key);
            ArgumentNullException.ThrowIfNull(source);

            // Replace any stale animation registered under the same key.
            if (_animations.TryGetValue(key, out var old))
            {
                _animations.Remove(key);
                old.Dispose();
            }

            var animation = new ConnectedAnimation(key, source, this);
            _animations[key] = animation;
            return animation;
        }

        /// <summary>
        /// Retrieves a pending animation registered under <paramref name="key"/>.
        /// Returns <see langword="null"/> if no animation exists or if it has already been consumed.
        /// Call this on the destination view <em>after</em> navigating, then call
        /// <c>TryStart</c> on the returned animation.
        /// </summary>
        public ConnectedAnimation? GetAnimation(string key)
        {
            if (_animations.TryGetValue(key, out var animation) && !animation.IsConsumed)
                return animation;

            return null;
        }

        /// <summary>
        /// Removes the animation registered under <paramref name="key"/>.
        /// The caller is responsible for disposing the animation separately.
        /// </summary>
        internal void RemoveAnimation(string key) => _animations.Remove(key);
    }
}

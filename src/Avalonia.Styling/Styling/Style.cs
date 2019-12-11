// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Metadata;

namespace Avalonia.Styling
{
    /// <summary>
    /// Defines a style.
    /// </summary>
    public class Style : AvaloniaObject, IStyle, ISetStyleParent
    {
        private static Dictionary<IStyleable, CompositeDisposable> _applied =
            new Dictionary<IStyleable, CompositeDisposable>();
        private IResourceNode _parent;

        private CompositeDisposable _subscriptions;

        private IResourceDictionary _resources;

        private IList<IAnimation> _animations;

        /// <summary>
        /// Initializes a new instance of the <see cref="Style"/> class.
        /// </summary>
        public Style()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Style"/> class.
        /// </summary>
        /// <param name="selector">The style selector.</param>
        public Style(Func<Selector, Selector> selector)
        {
            Selector = selector(null);
        }

        /// <inheritdoc/>
        public event EventHandler<ResourcesChangedEventArgs> ResourcesChanged;

        /// <summary>
        /// Gets or sets a dictionary of style resources.
        /// </summary>
        public IResourceDictionary Resources
        {
            get => _resources ?? (Resources = new ResourceDictionary());
            set
            {
                Contract.Requires<ArgumentNullException>(value != null);

                var hadResources = false;

                if (_resources != null)
                {
                    hadResources = _resources.Count > 0;
                    _resources.ResourcesChanged -= ResourceDictionaryChanged;
                }

                _resources = value;
                _resources.ResourcesChanged += ResourceDictionaryChanged;

                if (hadResources || _resources.Count > 0)
                {
                    ((ISetStyleParent)this).NotifyResourcesChanged(new ResourcesChangedEventArgs());
                }
            }
        }

        /// <summary>
        /// Gets or sets the style's selector.
        /// </summary>
        public Selector Selector { get; set; }

        /// <summary>
        /// Gets or sets the style's setters.
        /// </summary>
        [Content]
        public IList<ISetter> Setters { get; set; } = new List<ISetter>();

        public IList<IAnimation> Animations
        {
            get
            {
                return _animations ?? (_animations = new List<IAnimation>());
            }
        }

        private CompositeDisposable Subscriptions
        {
            get
            {
                return _subscriptions ?? (_subscriptions = new CompositeDisposable(2));
            }
        }

        /// <inheritdoc/>
        IResourceNode IResourceNode.ResourceParent => _parent;

        /// <inheritdoc/>
        bool IResourceProvider.HasResources => _resources?.Count > 0;

        /// <inheritdoc/>
        public bool Attach(IStyleable control, IStyleHost container)
        {
            if (Selector != null)
            {
                var match = Selector.Match(control);

                if (match.IsMatch)
                {
                    var controlSubscriptions = GetSubscriptions(control);
                    
                    var subs = new CompositeDisposable(Setters.Count + Animations.Count);

                    if (control is Animatable animatable)
                    {
                        foreach (var animation in Animations)
                        {
                            var obsMatch = match.Activator;

                            if (match.Result == SelectorMatchResult.AlwaysThisType ||
                                match.Result == SelectorMatchResult.AlwaysThisInstance)
                            {
                                obsMatch = Observable.Return(true);
                            }

                            var sub = animation.Apply(animatable, null, obsMatch);
                            subs.Add(sub);
                        } 
                    }

                    foreach (var setter in Setters)
                    {
                        var sub = setter.Apply(this, control, match.Activator);
                        subs.Add(sub);
                    }

                    controlSubscriptions.Add(subs);
                    controlSubscriptions.Add(Disposable.Create(() => Subscriptions.Remove(subs)));
                    Subscriptions.Add(subs);
                }

                return match.Result != SelectorMatchResult.NeverThisType;
            }
            else if (control == container)
            {
                var controlSubscriptions = GetSubscriptions(control);

                var subs = new CompositeDisposable(Setters.Count);

                foreach (var setter in Setters)
                {
                    var sub = setter.Apply(this, control, null);
                    subs.Add(sub);
                }

                controlSubscriptions.Add(subs);
                controlSubscriptions.Add(Disposable.Create(() => Subscriptions.Remove(subs)));
                Subscriptions.Add(subs);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public bool TryGetResource(object key, out object result)
        {
            result = null;
            return _resources?.TryGetResource(key, out result) ?? false;
        }

        /// <summary>
        /// Returns a string representation of the style.
        /// </summary>
        /// <returns>A string representation of the style.</returns>
        public override string ToString()
        {
            if (Selector != null)
            {
                return "Style: " + Selector.ToString();
            }
            else
            {
                return "Style";
            }
        }

        /// <inheritdoc/>
        void ISetStyleParent.NotifyResourcesChanged(ResourcesChangedEventArgs e)
        {
            ResourcesChanged?.Invoke(this, e);
        }

        /// <inheritdoc/>
        void ISetStyleParent.SetParent(IResourceNode parent)
        {
            if (_parent != null && parent != null)
            {
                throw new InvalidOperationException("The Style already has a parent.");
            }

            if (parent == null)
            {
                Detach();
            }

            _parent = parent;
        }

        public void Detach()
        {
            _subscriptions?.Dispose();
            _subscriptions = null;
        }

        private static CompositeDisposable GetSubscriptions(IStyleable control)
        {
            if (!_applied.TryGetValue(control, out var subscriptions))
            {
                subscriptions = new CompositeDisposable(3);
                subscriptions.Add(control.StyleDetach.Subscribe(ControlDetach));
                _applied.Add(control, subscriptions);
            }

            return subscriptions;
        }

        /// <summary>
        /// Called when a control's <see cref="IStyleable.StyleDetach"/> is signaled to remove
        /// all applied styles.
        /// </summary>
        /// <param name="control">The control.</param>
        private static void ControlDetach(IStyleable control)
        {
            var subscriptions = _applied[control];

            subscriptions.Dispose();

            _applied.Remove(control);
        }

        private void ResourceDictionaryChanged(object sender, ResourcesChangedEventArgs e)
        {
            ResourcesChanged?.Invoke(this, e);
        }
    }
}

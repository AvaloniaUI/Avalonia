// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Data;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Displays pages inside an <see cref="ItemsControl"/>.
    /// </summary>
    public class CarouselPresenter : ItemsPresenterBase
    {
        /// <summary>
        /// Defines the <see cref="IsVirtualized"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsVirtualizedProperty =
            Carousel.IsVirtualizedProperty.AddOwner<CarouselPresenter>();

        /// <summary>
        /// Defines the <see cref="SelectedIndex"/> property.
        /// </summary>
        public static readonly DirectProperty<CarouselPresenter, int> SelectedIndexProperty =
            SelectingItemsControl.SelectedIndexProperty.AddOwner<CarouselPresenter>(
                o => o.SelectedIndex,
                (o, v) => o.SelectedIndex = v);

        /// <summary>
        /// Defines the <see cref="Transition"/> property.
        /// </summary>
        public static readonly StyledProperty<IPageTransition> TransitionProperty =
            Carousel.TransitionProperty.AddOwner<CarouselPresenter>();

        private int _selectedIndex = -1;
        private Task _currentTransition;
        private int _queuedTransitionIndex = -1;

        /// <summary>
        /// Initializes static members of the <see cref="CarouselPresenter"/> class.
        /// </summary>
        static CarouselPresenter()
        {
            SelectedIndexProperty.Changed.AddClassHandler<CarouselPresenter>(x => x.SelectedIndexChanged);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the items in the carousel are virtualized.
        /// </summary>
        /// <remarks>
        /// When the carousel is virtualized, only the active page is held in memory.
        /// </remarks>
        public bool IsVirtualized
        {
            get { return GetValue(IsVirtualizedProperty); }
            set { SetValue(IsVirtualizedProperty, value); }
        }

        /// <summary>
        /// Gets or sets the index of the selected page.
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }

            set
            {
                var old = SelectedIndex;
                var effective = (value >= 0 && value < Items?.Cast<object>().Count()) ? value : -1;

                if (old != effective)
                {
                    _selectedIndex = effective;
                    RaisePropertyChanged(SelectedIndexProperty, old, effective, BindingPriority.LocalValue);
                }
            }
        }

        /// <summary>
        /// Gets or sets a transition to use when switching pages.
        /// </summary>
        public IPageTransition Transition
        {
            get { return GetValue(TransitionProperty); }
            set { SetValue(TransitionProperty, value); }
        }

        /// <inheritdoc/>
        protected override void PanelCreated(IPanel panel)
        {
#pragma warning disable 4014
            MoveToPage(-1, SelectedIndex);
#pragma warning restore 4014
        }

        /// <inheritdoc/>
        protected override void ItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            // TODO: Handle items changing.           
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    if (!IsVirtualized)
                    {
                        var generator = ItemContainerGenerator;
                        var containers = generator.RemoveRange(e.OldStartingIndex, e.OldItems.Count);
                        Panel.Children.RemoveAll(containers.Select(x => x.ContainerControl));
                    }
                    break;

            }
        }

        /// <summary>
        /// Moves to the selected page, animating if a <see cref="Transition"/> is set.
        /// </summary>
        /// <param name="fromIndex">The index of the old page.</param>
        /// <param name="toIndex">The index of the new page.</param>
        /// <returns>A task tracking the animation.</returns>
        private async Task MoveToPage(int fromIndex, int toIndex)
        {
            if (fromIndex != toIndex)
            {
                var generator = ItemContainerGenerator;
                IControl from = null;
                IControl to = null;

                if (fromIndex != -1)
                {
                    from = ItemContainerGenerator.ContainerFromIndex(fromIndex);
                }

                if (toIndex != -1)
                {
                    to = GetOrCreateContainer(toIndex);
                }

                if (Transition != null && (from != null || to != null))
                {
                    await Transition.Start((Visual)from, (Visual)to, fromIndex < toIndex);
                }
                else if (to != null)
                {
                    to.IsVisible = true;
                }

                if (from != null)
                {
                    if (IsVirtualized)
                    {
                        Panel.Children.Remove(from);
                        generator.Dematerialize(fromIndex, 1);
                    }
                    else
                    {
                        from.IsVisible = false;
                    }
                }
            }
        }

        private IControl GetOrCreateContainer(int index)
        {
            var container = ItemContainerGenerator.ContainerFromIndex(index);

            if (container == null)
            {
                var item = Items.Cast<object>().ElementAt(index);
                var materialized = ItemContainerGenerator.Materialize(index, item, MemberSelector);
                Panel.Children.Add(materialized.ContainerControl);
                container = materialized.ContainerControl;
            }

            return container;
        }

        /// <summary>
        /// Called when the <see cref="SelectedIndex"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private async void SelectedIndexChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (Panel != null)
            {
                if (_currentTransition == null)
                {
                    int fromIndex = (int)e.OldValue;
                    int toIndex = (int)e.NewValue;

                    for (;;)
                    {
                        _currentTransition = MoveToPage(fromIndex, toIndex);
                        await _currentTransition;

                        if (_queuedTransitionIndex != -1)
                        {
                            fromIndex = toIndex;
                            toIndex = _queuedTransitionIndex;
                            _queuedTransitionIndex = -1;
                        }
                        else
                        {
                            _currentTransition = null;
                            break;
                        }
                    }
                }
                else
                {
                    _queuedTransitionIndex = (int)e.NewValue;
                }
            }
        }
    }
}
// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Perspex.Animation;
using Perspex.Controls.Generators;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Controls.Utils;
using Perspex.Data;

namespace Perspex.Controls.Presenters
{
    /// <summary>
    /// Displays pages inside an <see cref="ItemsControl"/>.
    /// </summary>
    public class CarouselPresenter : Control, IItemsPresenter
    {
        /// <summary>
        /// Defines the <see cref="IsVirtualized"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsVirtualizedProperty =
            Carousel.IsVirtualizedProperty.AddOwner<CarouselPresenter>();

        /// <summary>
        /// Defines the <see cref="Items"/> property.
        /// </summary>
        public static readonly DirectProperty<CarouselPresenter, IEnumerable> ItemsProperty =
            ItemsControl.ItemsProperty.AddOwner<CarouselPresenter>(o => o.Items, (o, v) => o.Items = v);

        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplate<IPanel>> ItemsPanelProperty =
            ItemsControl.ItemsPanelProperty.AddOwner<CarouselPresenter>();
        
        /// <summary>
        /// Defines the <see cref="MemberSelector"/> property.
        /// </summary>
        public static readonly StyledProperty<IMemberSelector> MemberSelectorProperty =
            ItemsControl.MemberSelectorProperty.AddOwner<CarouselPresenter>();

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

        private IEnumerable _items;
        private int _selectedIndex = -1;
        private bool _createdPanel;
        private IItemContainerGenerator _generator;
        private Task _currentTransition;
        private int _queuedTransitionIndex = -1;

        /// <summary>
        /// Initializes static members of the <see cref="CarouselPresenter"/> class.
        /// </summary>
        static CarouselPresenter()
        {
            SelectedIndexProperty.Changed.AddClassHandler<CarouselPresenter>(x => x.SelectedIndexChanged);
            TemplatedParentProperty.Changed.AddClassHandler<CarouselPresenter>(x => x.TemplatedParentChanged);
        }

        /// <summary>
        /// Gets the <see cref="IItemContainerGenerator"/> used to generate item container
        /// controls.
        /// </summary>
        public IItemContainerGenerator ItemContainerGenerator
        {
            get
            {
                if (_generator == null)
                {
                    var i = TemplatedParent as ItemsControl;
                    _generator = i?.ItemContainerGenerator ?? new ItemContainerGenerator(this);
                }

                return _generator;
            }

            set
            {
                if (_generator != null)
                {
                    throw new InvalidOperationException("ItemContainerGenerator is already set.");
                }

                _generator = value;
            }
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
        /// Gets or sets the items to display.
        /// </summary>
        public IEnumerable Items
        {
            get { return _items; }
            set { SetAndRaise(ItemsProperty, ref _items, value); }
        }

        /// <summary>
        /// Gets or sets the panel used to display the pages.
        /// </summary>
        public ITemplate<IPanel> ItemsPanel
        {
            get { return GetValue(ItemsPanelProperty); }
            set { SetValue(ItemsPanelProperty, value); }
        }

        /// <summary>
        /// Selects a member from <see cref="Items"/> to use as the list item.
        /// </summary>
        public IMemberSelector MemberSelector
        {
            get { return GetValue(MemberSelectorProperty); }
            set { SetValue(MemberSelectorProperty, value); }
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
        /// Gets the panel used to display the pages.
        /// </summary>
        public IPanel Panel
        {
            get;
            private set;
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
        public override sealed void ApplyTemplate()
        {
            if (!_createdPanel)
            {
                CreatePanel();
            }
        }

        /// <summary>
        /// Creates the <see cref="Panel"/>.
        /// </summary>
        private void CreatePanel()
        {
            Panel = ItemsPanel.Build();
            Panel.SetValue(TemplatedParentProperty, TemplatedParent);

            LogicalChildren.Clear();
            VisualChildren.Clear();
            LogicalChildren.Add(Panel);
            VisualChildren.Add(Panel);

            _createdPanel = true;
            var task = MoveToPage(-1, SelectedIndex);
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
                    from = generator.ContainerFromIndex(fromIndex);
                }

                if (toIndex != -1)
                {
                    var item = Items.Cast<object>().ElementAt(toIndex);
                    to = generator.ContainerFromIndex(toIndex);

                    if (to == null)
                    {
                        to = generator.Materialize(toIndex, new[] { item }, MemberSelector)
                           .FirstOrDefault()?.ContainerControl;

                        if (to != null)
                        {
                            Panel.Children.Add(to);
                        }
                    }
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

        /// <summary>
        /// Called when the <see cref="SelectedIndex"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private async void SelectedIndexChanged(PerspexPropertyChangedEventArgs e)
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

        private void TemplatedParentChanged(PerspexPropertyChangedEventArgs e)
        {
            (e.NewValue as IItemsPresenterHost)?.RegisterItemsPresenter(this);
        }
    }
}

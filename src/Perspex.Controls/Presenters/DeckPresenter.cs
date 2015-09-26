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

namespace Perspex.Controls.Presenters
{
    /// <summary>
    /// Displays pages inside an <see cref="ItemsControl"/>.
    /// </summary>
    public class DeckPresenter : Control, IItemsPresenter
    {
        /// <summary>
        /// Defines the <see cref="Items"/> property.
        /// </summary>
        public static readonly PerspexProperty<IEnumerable> ItemsProperty =
            ItemsControl.ItemsProperty.AddOwner<DeckPresenter>();

        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly PerspexProperty<ITemplate<IPanel>> ItemsPanelProperty =
            ItemsControl.ItemsPanelProperty.AddOwner<DeckPresenter>();
        
        /// <summary>
        /// Defines the <see cref="MemberSelector"/> property.
        /// </summary>
        public static readonly PerspexProperty<IMemberSelector> MemberSelectorProperty =
            ItemsControl.MemberSelectorProperty.AddOwner<DeckPresenter>();

        /// <summary>
        /// Defines the <see cref="SelectedIndex"/> property.
        /// </summary>
        public static readonly PerspexProperty<int> SelectedIndexProperty =
            SelectingItemsControl.SelectedIndexProperty.AddOwner<DeckPresenter>();

        /// <summary>
        /// Defines the <see cref="Transition"/> property.
        /// </summary>
        public static readonly PerspexProperty<IPageTransition> TransitionProperty =
            Deck.TransitionProperty.AddOwner<DeckPresenter>();

        private bool _createdPanel;

        private IItemContainerGenerator _generator;

        /// <summary>
        /// Initializes static members of the <see cref="DeckPresenter"/> class.
        /// </summary>
        static DeckPresenter()
        {
            SelectedIndexProperty.Changed.AddClassHandler<DeckPresenter>(x => x.SelectedIndexChanged);
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
        /// Gets or sets the items to display.
        /// </summary>
        public IEnumerable Items
        {
            get { return GetValue(ItemsProperty); }
            set { SetValue(ItemsProperty, value); }
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
            get { return GetValue(SelectedIndexProperty); }
            set { SetValue(SelectedIndexProperty, value); }
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
            var logicalHost = this.FindReparentingHost();

            ClearVisualChildren();
            Panel = ItemsPanel.Build();
            Panel.SetValue(TemplatedParentProperty, TemplatedParent);

            AddVisualChild(Panel);

            if (logicalHost != null)
            {
                ((IReparentingControl)Panel).ReparentLogicalChildren(
                    logicalHost,
                    logicalHost.LogicalChildren);
            }

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
                to = generator.CreateContainers(toIndex, new[] { item }, MemberSelector).FirstOrDefault();

                if (to != null)
                {
                    Panel.Children.Add(to);
                }
            }

            if (Transition != null)
            {
                await Transition.Start((Visual)from, (Visual)to, fromIndex < toIndex);
            }

            if (from != null)
            {
                Panel.Children.Remove(from);
                generator.RemoveContainers(fromIndex, new[] { from });
            }
        }

        /// <summary>
        /// Called when the <see cref="SelectedIndex"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void SelectedIndexChanged(PerspexPropertyChangedEventArgs e)
        {
            if (Panel != null)
            {
                var task = MoveToPage((int)e.OldValue, (int)e.NewValue);
            }
        }
    }
}

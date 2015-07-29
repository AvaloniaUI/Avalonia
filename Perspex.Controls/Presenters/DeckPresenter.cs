// -----------------------------------------------------------------------
// <copyright file="DeckPresenter.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Presenters
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using Perspex.Animation;
    using Collections;
    using Perspex.Controls.Generators;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Utils;
    using Perspex.Styling;

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
        public static readonly PerspexProperty<ItemsPanelTemplate> ItemsPanelProperty =
            ItemsControl.ItemsPanelProperty.AddOwner<DeckPresenter>();

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

        private bool createdPanel;

        private IItemContainerGenerator generator;

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
                if (this.generator == null)
                {
                    var i = this.TemplatedParent as ItemsControl;
                    this.generator = i?.ItemContainerGenerator ?? new ItemContainerGenerator(this);
                }

                return this.generator;
            }

            set
            {
                if (this.generator != null)
                {
                    throw new InvalidOperationException("ItemContainerGenerator is already set.");
                }

                this.generator = value;
            }
        }

        /// <summary>
        /// Gets or sets the items to display.
        /// </summary>
        public IEnumerable Items
        {
            get { return this.GetValue(ItemsProperty); }
            set { this.SetValue(ItemsProperty, value); }
        }

        /// <summary>
        /// Gets or sets the panel used to display the pages.
        /// </summary>
        public ItemsPanelTemplate ItemsPanel
        {
            get { return this.GetValue(ItemsPanelProperty); }
            set { this.SetValue(ItemsPanelProperty, value); }
        }

        /// <summary>
        /// Gets or sets the index of the selected page.
        /// </summary>
        public int SelectedIndex
        {
            get { return this.GetValue(SelectedIndexProperty); }
            set { this.SetValue(SelectedIndexProperty, value); }
        }

        /// <summary>
        /// Gets the panel used to display the pages.
        /// </summary>
        public Panel Panel
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a transition to use when switching pages.
        /// </summary>
        public IPageTransition Transition
        {
            get { return this.GetValue(TransitionProperty); }
            set { this.SetValue(TransitionProperty, value); }
        }

        Panel IItemsPresenter.Panel
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <inheritdoc/>
        public override sealed void ApplyTemplate()
        {
            if (!this.createdPanel)
            {
                this.CreatePanel();
            }
        }

        /// <inheritdoc/>
        void IReparentingControl.ReparentLogicalChildren(ILogical logicalParent, IPerspexList<ILogical> children)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates the <see cref="Panel"/>.
        /// </summary>
        private void CreatePanel()
        {
            this.ClearVisualChildren();

            if (this.ItemsPanel != null)
            {
                this.Panel = this.ItemsPanel.Build();
                this.Panel.TemplatedParent = this.TemplatedParent;
                this.AddVisualChild(this.Panel);
                this.createdPanel = true;
                var task = this.MoveToPage(-1, this.SelectedIndex);
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
            var generator = this.ItemContainerGenerator;
            IControl from = null;
            IControl to = null;

            if (fromIndex != -1)
            {
                from = generator.ContainerFromIndex(fromIndex);
            }

            if (toIndex != -1)
            {
                var item = this.Items.Cast<object>().ElementAt(toIndex);
                to = generator.CreateContainers(toIndex, new[] { item }, null).FirstOrDefault();

                if (to != null)
                {
                    this.Panel.Children.Add(to);
                }
            }

            if (this.Transition != null)
            {
                await this.Transition.Start((Visual)from, (Visual)to, fromIndex < toIndex);
            }

            if (from != null)
            {
                this.Panel.Children.Remove(from);
                generator.RemoveContainers(fromIndex, new[] { from });
            }
        }

        /// <summary>
        /// Called when the <see cref="SelectedIndex"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void SelectedIndexChanged(PerspexPropertyChangedEventArgs e)
        {
            if (this.Panel != null)
            {
                var task = this.MoveToPage((int)e.OldValue, (int)e.NewValue);
            }
        }
    }
}

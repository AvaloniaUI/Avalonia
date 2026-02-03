using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Avalonia.Automation.Peers
{
    /// <summary>
    /// An automation peer for <see cref="PipsPager"/>.
    /// </summary>
    public class PipsPagerAutomationPeer : ControlAutomationPeer, ISelectionProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PipsPagerAutomationPeer"/> class.
        /// </summary>
        /// <param name="owner">The control associated with this peer.</param>
        public PipsPagerAutomationPeer(PipsPager owner) : base(owner)
        {
            owner.SelectedIndexChanged += OnSelectionChanged;
        }

        /// <summary>
        /// Gets the owner as a <see cref="PipsPager"/>.
        /// </summary>
        private new PipsPager Owner => (PipsPager)base.Owner;

        /// <inheritdoc/>
        public bool CanSelectMultiple => false;

        /// <inheritdoc/>
        public bool IsSelectionRequired => true;

        /// <inheritdoc/>
        public IReadOnlyList<AutomationPeer> GetSelection()
        {
            var result = new List<AutomationPeer>();
            var owner = Owner;

            if (owner.SelectedPageIndex >= 0 && owner.SelectedPageIndex < owner.NumberOfPages)
            {
                // Try to get the container for the selected index
                var pipsControl = owner.GetTemplateChildren()
                    .OfType<ItemsControl>()
                    .FirstOrDefault(x => x.Name == "PART_PipsPagerList");

                if (pipsControl != null)
                {
                    var container = pipsControl.ContainerFromIndex(owner.SelectedPageIndex);
                    if (container is Control c)
                    {
                        var peer = GetOrCreate(c);
                        result.Add(peer);
                    }
                }
            }

            return result;
        }

        /// <inheritdoc/>
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Menu;
        }

        /// <inheritdoc/>
        protected override string GetClassNameCore()
        {
            return nameof(PipsPager);
        }

        /// <inheritdoc/>
        protected override string? GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrWhiteSpace(name) ? "Pips Pager" : name;
        }

        private void OnSelectionChanged(object? sender, Controls.PipsPagerSelectedIndexChangedEventArgs e)
        {
            RaisePropertyChangedEvent(
                SelectionPatternIdentifiers.SelectionProperty,
                e.OldIndex,
                e.NewIndex);
        }
    }
}

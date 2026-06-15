using System.Collections.Generic;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    /// <summary>
    /// An automation peer for <see cref="PipsPager"/>.
    /// </summary>
    public class PipsPagerAutomationPeer : ControlAutomationPeer, ISelectionProvider
    {
        private ListBox? _pipsList;

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
                _pipsList ??= owner.FindNameScope()?.Find<ListBox>("PART_PipsPagerList");

                if (_pipsList != null)
                {
                    var container = _pipsList.ContainerFromIndex(owner.SelectedPageIndex);
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
            return AutomationControlType.List;
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

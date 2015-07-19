// -----------------------------------------------------------------------
// <copyright file="LogicalTreeViewModel.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Diagnostics.ViewModels
{
    using System.Reactive.Linq;
    using Perspex.Controls;
    using ReactiveUI;

    internal class LogicalTreeViewModel : ReactiveObject
    {
        private LogicalTreeNode selected;

        private ObservableAsPropertyHelper<ControlDetailsViewModel> details;

        public LogicalTreeViewModel(Control root)
        {
            this.Nodes = LogicalTreeNode.Create(root);
            this.details = this.WhenAnyValue(x => x.SelectedNode)
                .Select(x => x != null ? new ControlDetailsViewModel(x.Control) : null)
                .ToProperty(this, x => x.Details);
        }

        public LogicalTreeNode[] Nodes { get; }

        public LogicalTreeNode SelectedNode
        {
            get { return this.selected; }
            set { this.RaiseAndSetIfChanged(ref this.selected, value); }
        }

        public ControlDetailsViewModel Details
        {
            get { return this.details.Value; }
        }
    }
}

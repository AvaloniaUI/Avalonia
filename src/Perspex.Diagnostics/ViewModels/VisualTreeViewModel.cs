





namespace Perspex.Diagnostics.ViewModels
{
    using System.Reactive.Linq;
    using Perspex.Controls;
    using ReactiveUI;

    internal class VisualTreeViewModel : ReactiveObject
    {
        private VisualTreeNode selected;

        private ObservableAsPropertyHelper<ControlDetailsViewModel> details;

        public VisualTreeViewModel(Control root)
        {
            this.Nodes = VisualTreeNode.Create(root);
            this.details = this.WhenAnyValue(x => x.SelectedNode)
                .Select(x => x != null ? new ControlDetailsViewModel(x.Control) : null)
                .ToProperty(this, x => x.Details);
        }

        public VisualTreeNode[] Nodes { get; }

        public VisualTreeNode SelectedNode
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

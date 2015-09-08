// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reactive.Linq;
using Perspex.Controls;
using ReactiveUI;

namespace Perspex.Diagnostics.ViewModels
{
    internal class VisualTreeViewModel : ReactiveObject
    {
        private VisualTreeNode _selected;

        private ObservableAsPropertyHelper<ControlDetailsViewModel> _details;

        public VisualTreeViewModel(Control root)
        {
            Nodes = VisualTreeNode.Create(root);
            _details = this.WhenAnyValue(x => x.SelectedNode)
                .Select(x => x != null ? new ControlDetailsViewModel(x.Control) : null)
                .ToProperty(this, x => x.Details);
        }

        public VisualTreeNode[] Nodes { get; }

        public VisualTreeNode SelectedNode
        {
            get { return _selected; }
            set { this.RaiseAndSetIfChanged(ref _selected, value); }
        }

        public ControlDetailsViewModel Details
        {
            get { return _details.Value; }
        }
    }
}

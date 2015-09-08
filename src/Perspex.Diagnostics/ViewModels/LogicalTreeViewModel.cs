// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reactive.Linq;
using Perspex.Controls;
using ReactiveUI;

namespace Perspex.Diagnostics.ViewModels
{
    internal class LogicalTreeViewModel : ReactiveObject
    {
        private LogicalTreeNode _selected;

        private ObservableAsPropertyHelper<ControlDetailsViewModel> _details;

        public LogicalTreeViewModel(Control root)
        {
            Nodes = LogicalTreeNode.Create(root);
            _details = this.WhenAnyValue(x => x.SelectedNode)
                .Select(x => x != null ? new ControlDetailsViewModel(x.Control) : null)
                .ToProperty(this, x => x.Details);
        }

        public LogicalTreeNode[] Nodes { get; }

        public LogicalTreeNode SelectedNode
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

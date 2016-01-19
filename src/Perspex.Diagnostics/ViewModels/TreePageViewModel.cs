// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reactive.Linq;
using ReactiveUI;

namespace Perspex.Diagnostics.ViewModels
{
    internal class TreePageViewModel : ReactiveObject
    {
        private TreeNode _selected;

        private readonly ObservableAsPropertyHelper<ControlDetailsViewModel> _details;

        public TreePageViewModel(TreeNode[] nodes)
        {
            Nodes = nodes;
            _details = this.WhenAnyValue(x => x.SelectedNode)
                .Select(x => x != null ? new ControlDetailsViewModel(x.Control) : null)
                .ToProperty(this, x => x.Details);
        }

        public TreeNode[] Nodes { get; protected set; }

        public TreeNode SelectedNode
        {
            get { return _selected; }
            set { this.RaiseAndSetIfChanged(ref _selected, value); }
        }

        public ControlDetailsViewModel Details => _details.Value;
    }
}

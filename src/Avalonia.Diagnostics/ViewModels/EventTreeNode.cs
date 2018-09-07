// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Diagnostics;
using Avalonia.Collections;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal abstract class EventTreeNode : ViewModelBase
    {
        internal bool _updateChildren = true;
        internal bool _updateParent = true;
        private bool _isExpanded;
        private bool? _isEnabled = false;

        public EventTreeNode(EventTreeNode parent, string text)
        {
            this.Parent = parent;
            this.Text = text;
        }

        public IAvaloniaReadOnlyList<EventTreeNode> Children
        {
            get;
            protected set;
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { RaiseAndSetIfChanged(ref _isExpanded, value); }
        }

        public virtual bool? IsEnabled
        {
            get { return _isEnabled; }
            set { RaiseAndSetIfChanged(ref _isEnabled, value); }
        }

        public EventTreeNode Parent
        {
            get;
        }

        public string Text
        {
            get;
            private set;
        }

        internal void UpdateChecked()
        {
            IsEnabled = GetValue();

            bool? GetValue()
            {
                if (Children == null)
                    return false;
                bool? value = false;
                for (int i = 0; i < Children.Count; i++)
                {
                    if (i == 0)
                    {
                        value = Children[i].IsEnabled;
                        continue;
                    }

                    if (value != Children[i].IsEnabled)
                    {
                        value = null;
                        break;
                    }
                }

                return value;
            }
        }
    }
}

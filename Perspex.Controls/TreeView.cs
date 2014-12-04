// -----------------------------------------------------------------------
// <copyright file="TreeView.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System.Collections;
    using System.Collections.Generic;
    using Perspex.Controls.Generators;
    using Perspex.Controls.Primitives;
    using Perspex.Input;

    public class TreeView : SelectingItemsControl
    {
        protected override ItemContainerGenerator CreateItemContainerGenerator()
        {
            return new TreeItemContainerGenerator<TreeViewItem>(this);
        }

        protected override void MoveSelection(FocusNavigationDirection direction)
        {
            // TODO: Up and down movement is a *HACK* and probably pretty slow. Probably needs
            // rewriting at some point.
            if (this.SelectedItem != null)
            {
                switch (direction)
                {
                    case FocusNavigationDirection.Up:
                        {
                            var list = this.Flatten();
                            var index = list.IndexOf(this.SelectedItem);

                            if (index > 0)
                            {
                                this.SelectedItem = list[index - 1];
                            }
                            break;
                        }

                    case FocusNavigationDirection.Down:
                        {
                            var list = this.Flatten();
                            var index = list.IndexOf(this.SelectedItem);

                            if (index + 1 < list.Count)
                            {
                                this.SelectedItem = list[index + 1];
                            }
                            break;
                        }

                    case FocusNavigationDirection.Left:
                        {
                            var node = (TreeViewItem)this.ItemContainerGenerator.GetContainerForItem(this.SelectedItem);
                            node.IsExpanded = false;
                            break;
                        }

                    case FocusNavigationDirection.Right:
                        {
                            var node = (TreeViewItem)this.ItemContainerGenerator.GetContainerForItem(this.SelectedItem);
                            node.IsExpanded = true;
                            break;
                        }
                }
            }
        }

        List<object> Flatten()
        {
            var result = new List<object>();
            this.Flatten(this.Items, result);
            return result;
        }

        void Flatten(IEnumerable items, List<object> result)
        {
            if (items != null)
            {
                foreach (object item in items)
                {
                    var container = (TreeViewItem)this.ItemContainerGenerator.GetContainerForItem(item);
                    result.Add(item);

                    if (container.IsExpanded)
                    {
                        this.Flatten(container.Items, result);
                    }
                }
            }
        }
    }
}

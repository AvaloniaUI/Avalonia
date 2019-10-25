using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class StyleTreeNode : TreeNode
    {
        public static IAvaloniaReadOnlyList<TreeNode> WithStyles<T>(TreeNode node, IAvaloniaReadOnlyList<T> children) where T : TreeNode
        {
            if (node.Visual is Control ctrl && (ctrl.Styles.Count > 0 || ctrl is TopLevel))
            {
                var result = new AvaloniaList<TreeNode>();
                if (ctrl is TopLevel)
                {
                    result.Add(new StyleTreeNode(Application.Current.Styles, node));
                }
                if (ctrl.Styles.Count > 0)
                {
                    result.Add(new StyleTreeNode(ctrl.Styles, node));
                }
                var cnt = result.Count;
                children.ForEachItem((i, v) => result.Insert(i + cnt, v),
                                    (i, v) => result.RemoveAt(i + cnt),
                                    () => result.RemoveRange(cnt, result.Count - cnt), 
                                    true);
                return result;
            }
            return children;
        }
        public StyleTreeNode(IStyle style, TreeNode parent) : base((IAvaloniaObject)style, parent)
        {
            Children = new AvaloniaList<TreeNode>(((style as Styles)?.OfType<Style>().Select(s => new StyleTreeNode(s, this))) ?? Array.Empty<StyleTreeNode>());
        }
    }
}

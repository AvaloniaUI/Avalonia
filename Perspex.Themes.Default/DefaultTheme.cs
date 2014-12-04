// -----------------------------------------------------------------------
// <copyright file="DefaultTheme.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using Perspex.Styling;

    public class DefaultTheme : Styles
    {
        public DefaultTheme()
        {
            this.Add(new ButtonStyle());
            this.Add(new CheckBoxStyle());
            this.Add(new ContentControlStyle());
            this.Add(new GridSplitterStyle());
            this.Add(new ItemsControlStyle());
            this.Add(new ListBoxStyle());
            this.Add(new ListBoxItemStyle());
            this.Add(new ScrollBarStyle());
            this.Add(new ScrollViewerStyle());
            this.Add(new TabControlStyle());
            this.Add(new TabItemStyle());
            this.Add(new TabStripStyle());
            this.Add(new TextBoxStyle());
            this.Add(new TreeViewStyle());
            this.Add(new TreeViewItemStyle());
            this.Add(new WindowStyle());
        }
    }
}

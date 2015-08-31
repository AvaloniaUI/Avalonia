// -----------------------------------------------------------------------
// <copyright file="DefaultTheme.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using Perspex.Styling;

    /// <summary>
    /// The default Perspex theme.
    /// </summary>
    public class DefaultTheme : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultTheme"/> class.
        /// </summary>
        public DefaultTheme()
        {
            this.Add(new FocusAdornerStyle());

            this.Add(new ButtonStyle());
            this.Add(new CheckBoxStyle());
            this.Add(new ContentControlStyle());
            this.Add(new DeckStyle());
            this.Add(new DropDownStyle());
            this.Add(new GridSplitterStyle());
            this.Add(new ItemsControlStyle());
            this.Add(new ListBoxStyle());
            this.Add(new ListBoxItemStyle());
            this.Add(new MenuStyle());
            this.Add(new MenuItemStyle());
            this.Add(new PopupRootStyle());
            this.Add(new ProgressBarStyle());
            this.Add(new RadioButtonStyle());
            this.Add(new ScrollBarStyle());
            this.Add(new ScrollViewerStyle());
            this.Add(new TabControlStyle());
            this.Add(new TabItemStyle());
            this.Add(new TabStripStyle());
            this.Add(new TextBoxStyle());
            this.Add(new ToggleButtonStyle());
            this.Add(new ToolTipStyle());
            this.Add(new TreeViewStyle());
            this.Add(new TreeViewItemStyle());
            this.Add(new WindowStyle());
        }
    }
}

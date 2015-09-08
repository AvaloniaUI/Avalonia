// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Styling;

namespace Perspex.Themes.Default
{
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

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
            Add(new FocusAdornerStyle());

            Add(new ButtonStyle());
            Add(new CheckBoxStyle());
            Add(new ContentControlStyle());
            Add(new DeckStyle());
            Add(new DropDownStyle());
            Add(new GridSplitterStyle());
            Add(new ItemsControlStyle());
            Add(new ListBoxStyle());
            Add(new ListBoxItemStyle());
            Add(new MenuStyle());
            Add(new MenuItemStyle());
            Add(new PopupRootStyle());
            Add(new ProgressBarStyle());
            Add(new RadioButtonStyle());
            Add(new ScrollBarStyle());
            Add(new ScrollViewerStyle());
            Add(new TabControlStyle());
            Add(new TabItemStyle());
            Add(new TabStripStyle());
            Add(new TextBoxStyle());
            Add(new ToggleButtonStyle());
            Add(new ToolTipStyle());
            Add(new TreeViewStyle());
            Add(new TreeViewItemStyle());
            Add(new WindowStyle());
        }
    }
}

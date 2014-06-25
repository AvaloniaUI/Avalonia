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
            this.Add(new TextBoxStyle());

        }
    }
}

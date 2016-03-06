// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Markup.Xaml;
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
            PerspexXamlLoader.Load(this);
        }
    }
}

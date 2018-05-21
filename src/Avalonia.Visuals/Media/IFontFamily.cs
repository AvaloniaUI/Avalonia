using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Media
{
    using Avalonia.Media.Fonts;

    public interface IFontFamily
    {
        /// <summary>
        /// Gets the name of the font family.
        /// </summary>
        /// <value>
        /// The name of the font family.
        /// </value>
        string Name { get; }

        /// <summary>
        /// Gets the family names.
        /// </summary>
        /// <value>
        /// The family familyNames.
        /// </value>
        FamilyNameCollection FamilyNames { get; }

        /// <summary>
        /// Gets the key for associated assets.
        /// </summary>
        /// <value>
        /// The family familyNames.
        /// </value>
        FontFamilyKey Key { get; }
    }
}

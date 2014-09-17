// -----------------------------------------------------------------------
// <copyright file="DefinitionBase.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    public class DefinitionBase : PerspexObject
    {
        public static readonly PerspexProperty<string> SharedSizeGroupProperty =
            PerspexProperty.Register<DefinitionBase, string>("SharedSizeGroup", inherits: true);

        public string SharedSizeGroup
        {
            get { return this.GetValue(SharedSizeGroupProperty); }
            set { this.SetValue(SharedSizeGroupProperty, value); }
        }
    }
}

// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Collections;

namespace Perspex.Styling
{
    public class Styles : PerspexList<IStyle>, IStyle
    {
        public void Attach(IStyleable control)
        {
            foreach (IStyle style in this)
            {
                style.Attach(control);
            }
        }
    }
}

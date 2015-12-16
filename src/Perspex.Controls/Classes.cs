// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Perspex.Collections;

namespace Perspex.Controls
{
    public class Classes : PerspexList<string>
    {
        public Classes()
        {            
        }

        public Classes(IEnumerable<string> items)
            : base(items)
        {
        }

        public Classes(params string[] items)
            : base(items)
        {            
        }

        public override void Add(string item)
        {
            if (!Contains(item))
            {
                base.Add(item);
            }
        }

        public override void AddRange(IEnumerable<string> items)
        {
            base.AddRange(items.Where(x => !Contains(x)));
        }
    }
}

// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Perspex.Rendering;

namespace Perspex.SceneGraph.UnitTests
{
    public class ParamEventArgs<T> : EventArgs
    {
        public ParamEventArgs(T param)
        {
            Param = param;
        }

        public T Param { get; set; }
    }

    public class TestVisual : Visual
    {
        public new PerspexObject InheritanceParent => base.InheritanceParent;

        public IVisual Child
        {
            get
            {
                return ((IVisual)this).VisualChildren.FirstOrDefault();
            }

            set
            {
                if (Child != null)
                {
                    RemoveVisualChild(Child);
                }

                if (value != null)
                {
                    AddVisualChild(value);
                }
            }
        }

        public void AddChild(Visual v)
        {
            AddVisualChild(v);
        }

        public void AddChildren(IEnumerable<Visual> v)
        {
            AddVisualChildren(v);
        }

        public void RemoveChild(Visual v)
        {
            RemoveVisualChild(v);
        }

        public void ClearChildren()
        {
            ClearVisualChildren();
        }
    }
}

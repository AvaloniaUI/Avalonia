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
        public event EventHandler<ParamEventArgs<IRenderRoot>> AttachedToVisualTreeCalled;

        public event EventHandler<ParamEventArgs<IRenderRoot>> DetachedFromVisualTreeCalled;

        public new PerspexObject InheritanceParent => base.InheritanceParent;

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

        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            if (AttachedToVisualTreeCalled != null)
            {
                AttachedToVisualTreeCalled(this, new ParamEventArgs<IRenderRoot>(root));
            }
        }

        protected override void OnDetachedFromVisualTree(IRenderRoot oldRoot)
        {
            if (DetachedFromVisualTreeCalled != null)
            {
                DetachedFromVisualTreeCalled(this, new ParamEventArgs<IRenderRoot>(oldRoot));
            }
        }
    }
}

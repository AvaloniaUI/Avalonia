using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.VisualTree;

namespace Avalonia.Base.UnitTests
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
                    Children.VisualMutable.Remove(Child);
                }

                if (value != null)
                {
                    Children.VisualMutable.Add(value);
                }
            }
        }

        public void AddChild(Visual v)
        {
            Children.VisualMutable.Add(v);
        }

        public void AddChildren(IEnumerable<Visual> v)
        {
            ((IAvaloniaList<IVisual>)Children.VisualMutable).AddRange(v);
        }

        public void RemoveChild(Visual v)
        {
            Children.VisualMutable.Remove(v);
        }

        public void ClearChildren()
        {
            Children.VisualMutable.Clear();
        }
    }
}

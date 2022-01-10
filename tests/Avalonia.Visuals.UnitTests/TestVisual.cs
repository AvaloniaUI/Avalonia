using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;

namespace Avalonia.Visuals.UnitTests
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
        private List<IVisual> _children = new List<IVisual>();

        public IVisual Child
        {
            get => _children.FirstOrDefault();
            set
            {
                _children.Clear();
                if (value is not null)
                {
                    _children.Add(value);
                    AddVisualChild(value);
                }
            }
        }

        protected override int VisualChildrenCount => _children.Count;

        public void AddChild(Visual v)
        {
            _children.Add(v);
            AddVisualChild(v);
        }

        public void AddChildren(IEnumerable<Visual> v)
        {
            _children.AddRange(v);

            foreach (var i in v)
                AddVisualChild(i);
        }

        public void RemoveChild(Visual v)
        {
            _children.Remove(v);
            RemoveVisualChild(v);
        }

        public void ClearChildren()
        {
            foreach (var i in _children)
                RemoveVisualChild(i);
            _children.Clear();
        }

        protected override IVisual GetVisualChild(int index) => _children[index];
    }
}

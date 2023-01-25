using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

using Avalonia.Collections;

namespace Avalonia.Diagnostics.ViewModels
{
    internal abstract class TreeNodeCollection : IAvaloniaReadOnlyList<TreeNode>, IList, IDisposable
    {
        private class EmptyTreeNodeCollection : TreeNodeCollection
        {
            public EmptyTreeNodeCollection():base(default!)
            {

            }
            protected override void Initialize(AvaloniaList<TreeNode> nodes)
            {
                
            }
        }

        static readonly internal TreeNodeCollection Empty = new EmptyTreeNodeCollection();

        private AvaloniaList<TreeNode>? _inner;

        public TreeNodeCollection(TreeNode owner) => Owner = owner;

        public TreeNode this[int index] => EnsureInitialized()[index];

        public int Count => EnsureInitialized().Count;

        protected TreeNode Owner { get; }
        bool IList.IsFixedSize => false;
        bool IList.IsReadOnly => true;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => this;

        object? IList.this[int index] 
        {
            get => this[index];
            set => throw new NotImplementedException();
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add => EnsureInitialized().CollectionChanged += value;
            remove => EnsureInitialized().CollectionChanged -= value;
        }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => EnsureInitialized().PropertyChanged += value;
            remove => EnsureInitialized().PropertyChanged -= value;
        }

        public virtual void Dispose()
        {
            if (_inner is object)
            {
                foreach (var node in _inner)
                {
                    node.Dispose();
                }
            }
        }

        public IEnumerator<TreeNode> GetEnumerator()
        {
            return EnsureInitialized().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected abstract void Initialize(AvaloniaList<TreeNode> nodes);

        private AvaloniaList<TreeNode> EnsureInitialized()
        {
            if (_inner is null)
            {
                _inner = new AvaloniaList<TreeNode>();
                Initialize(_inner);
            }
            return _inner;
        }

        int IList.Add(object? value) => throw new NotImplementedException();
        void IList.Clear() => throw new NotImplementedException();
        bool IList.Contains(object? value) => EnsureInitialized().Contains((TreeNode)value!);
        int IList.IndexOf(object? value) => EnsureInitialized().IndexOf((TreeNode)value!);
        void IList.Insert(int index, object? value) => throw new NotImplementedException();
        void IList.Remove(object? value) => throw new NotImplementedException();
        void IList.RemoveAt(int index) => throw new NotImplementedException();
        void ICollection.CopyTo(Array array, int index) => throw new NotImplementedException();
    }
}

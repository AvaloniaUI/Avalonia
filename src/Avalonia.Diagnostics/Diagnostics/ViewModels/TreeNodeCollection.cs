using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Collections;

namespace Avalonia.Diagnostics.ViewModels
{
    internal abstract class TreeNodeCollection : IAvaloniaReadOnlyList<TreeNode>, IDisposable
    {
        private AvaloniaList<TreeNode> _inner;

        public TreeNodeCollection(TreeNode owner) => Owner = owner;

        public TreeNode this[int index]
        {
            get
            {
                EnsureInitialized();
                return _inner[index];
            }
        }

        public int Count
        {
            get
            {
                EnsureInitialized();
                return _inner.Count;
            }
        }

        protected TreeNode Owner { get; }

        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add => _inner.CollectionChanged += value;
            remove => _inner.CollectionChanged -= value;
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => _inner.PropertyChanged += value;
            remove => _inner.PropertyChanged -= value;
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
            EnsureInitialized();
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected abstract void Initialize(AvaloniaList<TreeNode> nodes);

        private void EnsureInitialized()
        {
            if (_inner is null)
            {
                _inner = new AvaloniaList<TreeNode>();
                Initialize(_inner);
            }
        }
    }
}

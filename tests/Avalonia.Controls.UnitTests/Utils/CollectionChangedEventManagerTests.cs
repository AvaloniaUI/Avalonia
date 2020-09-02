using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Avalonia.Collections;
using Avalonia.Controls.Utils;
using Xunit;
using CollectionChangedEventManager = Avalonia.Controls.Utils.CollectionChangedEventManager;

namespace Avalonia.Controls.UnitTests.Utils
{
    public class CollectionChangedEventManagerTests
    {
        [Fact]
        public void AddListener_Listens_To_Events()
        {
            var source = new AvaloniaList<string>();
            var listener = new Listener();

            CollectionChangedEventManager.Instance.AddListener(source, listener);

            Assert.Empty(listener.Received);

            source.Add("foo");

            Assert.Equal(1, listener.Received.Count);
        }

        [Fact]
        public void RemoveListener_Stops_Listening_To_Events()
        {
            var source = new AvaloniaList<string>();
            var listener = new Listener();

            CollectionChangedEventManager.Instance.AddListener(source, listener);
            CollectionChangedEventManager.Instance.RemoveListener(source, listener);

            source.Add("foo");

            Assert.Empty(listener.Received);
        }

        [Fact]
        public void Receives_Events_From_Wrapped_Collection()
        {
            var source = new WrappingCollection();
            var listener = new Listener();

            CollectionChangedEventManager.Instance.AddListener(source, listener);

            Assert.Empty(listener.Received);

            source.Add("foo");

            Assert.Equal(1, listener.Received.Count);
        }

        private class Listener : ICollectionChangedListener
        {
            public List<NotifyCollectionChangedEventArgs> Received { get; } = new List<NotifyCollectionChangedEventArgs>();

            public void Changed(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
            {
                Received.Add(e);
            }

            public void PostChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
            {
            }

            public void PreChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
            {
            }
        }

        private class WrappingCollection : INotifyCollectionChanged
        {
            private AvaloniaList<string> _inner = new AvaloniaList<string>();

            public void Add(string s) => _inner.Add(s);

            public event NotifyCollectionChangedEventHandler CollectionChanged
            {
                add => _inner.CollectionChanged += value;
                remove => _inner.CollectionChanged -= value;
            }
        }
    }
}

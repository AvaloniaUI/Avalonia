using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Avalonia.Collections;
using Avalonia.Diagnostics;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ItemsSourceViewTests
    {
        [Fact]
        public void Only_Subscribes_To_Source_CollectionChanged_When_CollectionChanged_Subscribed()
        {
            var source = new AvaloniaList<string>();
            var target = ItemsSourceView.GetOrCreate(source);
            var debug = (INotifyCollectionChangedDebug)source;

            Assert.Null(debug.GetCollectionChangedSubscribers());

            void Handler(object sender, NotifyCollectionChangedEventArgs e) { }
            target.CollectionChanged += Handler;

            Assert.NotNull(debug.GetCollectionChangedSubscribers());
            Assert.Equal(1, debug.GetCollectionChangedSubscribers().Length);

            target.CollectionChanged -= Handler;

            Assert.Null(debug.GetCollectionChangedSubscribers());
        }

        [Fact]
        public void Cannot_Create_ItemsSourceView_With_Collection_That_Implements_INCC_But_Not_List()
        {
            var source = new InvalidCollection();
            Assert.Throws<ArgumentException>(() => ItemsSourceView.GetOrCreate(source));
        }

        private class InvalidCollection : INotifyCollectionChanged, IEnumerable<string>
        {
            public event NotifyCollectionChangedEventHandler CollectionChanged { add { } remove { } }

            public IEnumerator<string> GetEnumerator()
            {
                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                yield break;
            }
        }
    }
}

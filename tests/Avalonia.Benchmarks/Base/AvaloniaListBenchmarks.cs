using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Base
{
    /// <summary>
    /// Benchmarks for AvaloniaList operations which are used extensively throughout the framework
    /// for observable collections (Children, Items, etc.).
    /// </summary>
    [MemoryDiagnoser]
    public class AvaloniaListBenchmarks
    {
        private AvaloniaList<int> _smallList = null!;
        private AvaloniaList<int> _mediumList = null!;
        private AvaloniaList<int> _largeList = null!;
        private List<int> _standardSmallList = null!;
        private List<int> _standardMediumList = null!;
        private List<int> _standardLargeList = null!;
        private int[] _itemsToAdd = null!;
        private NotifyCollectionChangedEventHandler _handler = null!;

        [GlobalSetup]
        public void Setup()
        {
            _smallList = new AvaloniaList<int>(Enumerable.Range(0, 10));
            _mediumList = new AvaloniaList<int>(Enumerable.Range(0, 100));
            _largeList = new AvaloniaList<int>(Enumerable.Range(0, 1000));

            _standardSmallList = new List<int>(Enumerable.Range(0, 10));
            _standardMediumList = new List<int>(Enumerable.Range(0, 100));
            _standardLargeList = new List<int>(Enumerable.Range(0, 1000));

            _itemsToAdd = Enumerable.Range(0, 10).ToArray();
            _handler = (s, e) => { };
        }

        #region Add Operations

        [Benchmark(Baseline = true)]
        public void AvaloniaList_Add_NoSubscribers()
        {
            var list = new AvaloniaList<int>();
            for (int i = 0; i < 100; i++)
            {
                list.Add(i);
            }
        }

        [Benchmark]
        public void AvaloniaList_Add_WithSubscriber()
        {
            var list = new AvaloniaList<int>();
            list.CollectionChanged += _handler;
            for (int i = 0; i < 100; i++)
            {
                list.Add(i);
            }
            list.CollectionChanged -= _handler;
        }

        [Benchmark]
        public void StandardList_Add()
        {
            var list = new List<int>();
            for (int i = 0; i < 100; i++)
            {
                list.Add(i);
            }
        }

        #endregion

        #region AddRange Operations

        [Benchmark]
        public void AvaloniaList_AddRange_NoSubscribers()
        {
            var list = new AvaloniaList<int>();
            list.AddRange(_itemsToAdd);
        }

        [Benchmark]
        public void AvaloniaList_AddRange_WithSubscriber()
        {
            var list = new AvaloniaList<int>();
            list.CollectionChanged += _handler;
            list.AddRange(_itemsToAdd);
            list.CollectionChanged -= _handler;
        }

        [Benchmark]
        public void StandardList_AddRange()
        {
            var list = new List<int>();
            list.AddRange(_itemsToAdd);
        }

        #endregion

        #region Insert Operations

        [Benchmark]
        public void AvaloniaList_Insert_AtStart()
        {
            var list = new AvaloniaList<int>(Enumerable.Range(0, 100));
            for (int i = 0; i < 10; i++)
            {
                list.Insert(0, i);
            }
        }

        [Benchmark]
        public void AvaloniaList_Insert_AtEnd()
        {
            var list = new AvaloniaList<int>(Enumerable.Range(0, 100));
            for (int i = 0; i < 10; i++)
            {
                list.Insert(list.Count, i);
            }
        }

        [Benchmark]
        public void AvaloniaList_InsertRange()
        {
            var list = new AvaloniaList<int>(Enumerable.Range(0, 100));
            list.InsertRange(50, _itemsToAdd);
        }

        #endregion

        #region Remove Operations

        [Benchmark]
        public void AvaloniaList_Remove_FromSmallList()
        {
            var list = new AvaloniaList<int>(Enumerable.Range(0, 10));
            list.Remove(5);
        }

        [Benchmark]
        public void AvaloniaList_RemoveAt_FromSmallList()
        {
            var list = new AvaloniaList<int>(Enumerable.Range(0, 10));
            list.RemoveAt(5);
        }

        [Benchmark]
        public void AvaloniaList_RemoveRange()
        {
            var list = new AvaloniaList<int>(Enumerable.Range(0, 100));
            list.RemoveRange(40, 20);
        }

        [Benchmark]
        public void AvaloniaList_RemoveAll()
        {
            var list = new AvaloniaList<int>(Enumerable.Range(0, 100));
            list.RemoveAll(Enumerable.Range(40, 20)); // Remove items 40-59
        }

        #endregion

        #region Clear Operations

        [Benchmark]
        public void AvaloniaList_Clear_ResetBehavior()
        {
            var list = new AvaloniaList<int>(Enumerable.Range(0, 100));
            list.ResetBehavior = ResetBehavior.Reset;
            list.Clear();
        }

        [Benchmark]
        public void AvaloniaList_Clear_RemoveBehavior()
        {
            var list = new AvaloniaList<int>(Enumerable.Range(0, 100));
            list.ResetBehavior = ResetBehavior.Remove;
            list.Clear();
        }

        #endregion

        #region Index/Contains Operations

        [Benchmark]
        public int AvaloniaList_IndexOf_SmallList()
        {
            return _smallList.IndexOf(5);
        }

        [Benchmark]
        public int AvaloniaList_IndexOf_MediumList()
        {
            return _mediumList.IndexOf(50);
        }

        [Benchmark]
        public int AvaloniaList_IndexOf_LargeList()
        {
            return _largeList.IndexOf(500);
        }

        [Benchmark]
        public bool AvaloniaList_Contains_SmallList()
        {
            return _smallList.Contains(5);
        }

        [Benchmark]
        public bool AvaloniaList_Contains_MediumList()
        {
            return _mediumList.Contains(50);
        }

        [Benchmark]
        public bool AvaloniaList_Contains_LargeList()
        {
            return _largeList.Contains(500);
        }

        #endregion

        #region Enumeration

        [Benchmark]
        public int AvaloniaList_Enumerate_SmallList()
        {
            int sum = 0;
            foreach (var item in _smallList)
            {
                sum += item;
            }
            return sum;
        }

        [Benchmark]
        public int AvaloniaList_Enumerate_MediumList()
        {
            int sum = 0;
            foreach (var item in _mediumList)
            {
                sum += item;
            }
            return sum;
        }

        [Benchmark]
        public int AvaloniaList_Enumerate_LargeList()
        {
            int sum = 0;
            foreach (var item in _largeList)
            {
                sum += item;
            }
            return sum;
        }

        [Benchmark]
        public int StandardList_Enumerate_SmallList()
        {
            int sum = 0;
            foreach (var item in _standardSmallList)
            {
                sum += item;
            }
            return sum;
        }

        [Benchmark]
        public int StandardList_Enumerate_MediumList()
        {
            int sum = 0;
            foreach (var item in _standardMediumList)
            {
                sum += item;
            }
            return sum;
        }

        [Benchmark]
        public int StandardList_Enumerate_LargeList()
        {
            int sum = 0;
            foreach (var item in _standardLargeList)
            {
                sum += item;
            }
            return sum;
        }

        #endregion

        #region Indexer Access

        [Benchmark]
        public int AvaloniaList_IndexerAccess_SmallList()
        {
            int sum = 0;
            for (int i = 0; i < _smallList.Count; i++)
            {
                sum += _smallList[i];
            }
            return sum;
        }

        [Benchmark]
        public int AvaloniaList_IndexerAccess_MediumList()
        {
            int sum = 0;
            for (int i = 0; i < _mediumList.Count; i++)
            {
                sum += _mediumList[i];
            }
            return sum;
        }

        [Benchmark]
        public int AvaloniaList_IndexerAccess_LargeList()
        {
            int sum = 0;
            for (int i = 0; i < _largeList.Count; i++)
            {
                sum += _largeList[i];
            }
            return sum;
        }

        #endregion

        #region Move Operations

        [Benchmark]
        public void AvaloniaList_Move()
        {
            var list = new AvaloniaList<int>(Enumerable.Range(0, 100));
            list.Move(10, 90);
        }

        [Benchmark]
        public void AvaloniaList_MoveRange()
        {
            var list = new AvaloniaList<int>(Enumerable.Range(0, 100));
            list.MoveRange(10, 5, 80);
        }

        #endregion

        #region Set Indexer

        [Benchmark]
        public void AvaloniaList_SetIndexer_NoSubscribers()
        {
            var list = new AvaloniaList<int>(Enumerable.Range(0, 100));
            for (int i = 0; i < 100; i++)
            {
                list[i] = i + 1;
            }
        }

        [Benchmark]
        public void AvaloniaList_SetIndexer_WithSubscriber()
        {
            var list = new AvaloniaList<int>(Enumerable.Range(0, 100));
            list.CollectionChanged += _handler;
            for (int i = 0; i < 100; i++)
            {
                list[i] = i + 1;
            }
            list.CollectionChanged -= _handler;
        }

        #endregion
    }
}

using System.Collections.Generic;
using Avalonia.Utilities;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Utilities
{
    [MemoryDiagnoser]
    public class FrugalListBenchmarks
    {
        [Params(1, 3, 6, 10, 50)]
        public int ItemCount { get; set; }

        [Benchmark(Baseline = true)]
        public int List_AddAndEnumerate()
        {
            var list = new List<int>(ItemCount);
            for (int i = 0; i < ItemCount; i++)
            {
                list.Add(i);
            }

            int sum = 0;
            foreach (var item in list)
            {
                sum += item;
            }
            return sum;
        }

        [Benchmark]
        public int FrugalObjectList_AddAndEnumerate()
        {
            var list = new FrugalObjectList<int>();
            for (int i = 0; i < ItemCount; i++)
            {
                list.Add(i);
            }

            int sum = 0;
            for (int i = 0; i < list.Count; i++)
            {
                sum += list[i];
            }
            return sum;
        }

        [Benchmark]
        public int FrugalStructList_AddAndEnumerate()
        {
            var list = new FrugalStructList<int>();
            for (int i = 0; i < ItemCount; i++)
            {
                list.Add(i);
            }

            int sum = 0;
            for (int i = 0; i < list.Count; i++)
            {
                sum += list[i];
            }
            return sum;
        }

        [Benchmark]
        public bool List_Contains()
        {
            var list = new List<int>(ItemCount);
            for (int i = 0; i < ItemCount; i++)
            {
                list.Add(i);
            }
            return list.Contains(ItemCount - 1); // Search for last item
        }

        [Benchmark]
        public bool FrugalObjectList_Contains()
        {
            var list = new FrugalObjectList<int>();
            for (int i = 0; i < ItemCount; i++)
            {
                list.Add(i);
            }
            return list.Contains(ItemCount - 1); // Search for last item
        }

        [Benchmark]
        public int List_IndexOf()
        {
            var list = new List<int>(ItemCount);
            for (int i = 0; i < ItemCount; i++)
            {
                list.Add(i);
            }
            return list.IndexOf(ItemCount - 1); // Search for last item
        }

        [Benchmark]
        public int FrugalObjectList_IndexOf()
        {
            var list = new FrugalObjectList<int>();
            for (int i = 0; i < ItemCount; i++)
            {
                list.Add(i);
            }
            return list.IndexOf(ItemCount - 1); // Search for last item
        }

        [Benchmark]
        public void List_RemoveAt()
        {
            var list = new List<int>(ItemCount);
            for (int i = 0; i < ItemCount; i++)
            {
                list.Add(i);
            }

            // Remove from middle
            if (list.Count > 0)
            {
                list.RemoveAt(list.Count / 2);
            }
        }

        [Benchmark]
        public void FrugalObjectList_RemoveAt()
        {
            var list = new FrugalObjectList<int>();
            for (int i = 0; i < ItemCount; i++)
            {
                list.Add(i);
            }

            // Remove from middle
            if (list.Count > 0)
            {
                list.RemoveAt(list.Count / 2);
            }
        }
    }
}

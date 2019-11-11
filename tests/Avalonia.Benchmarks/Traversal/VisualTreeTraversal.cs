using System.Collections.Generic;
using Avalonia.Benchmarks.Layout;
using Avalonia.Controls;
using Avalonia.Traversal;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Traversal
{
    [MemoryDiagnoser]
    public class VisualTreeTraversal
    {
        private readonly TestRoot _root;
        private readonly List<Control> _controls = new List<Control>();

        public VisualTreeTraversal()
        {
            var panel = new StackPanel();
            _root = new TestRoot { Child = panel, Renderer = new NullRenderer()};
            _controls.Add(panel);
            _controls = ControlHierarchyCreator.CreateChildren(_controls, panel, 3, 5, 5);
            _root.LayoutManager.ExecuteInitialLayoutPass(_root);
        }

        [Benchmark]
        public void Visit_Struct()
        {
            VisualTreeOperations.VisitDescendants<InvalidateVisualVisitor>(_root, TreeVisitMode.IncludeSelf);
        }

        [Benchmark]
        public void Visit_Lambda()
        {
            VisualTreeOperations.VisitDescendants(_root, visual =>
            {
                visual.InvalidateVisual();

                return TreeVisit.Continue;
            }, TreeVisitMode.IncludeSelf);
        }

        [Benchmark(Baseline = true)]
        public void Visit_IEnumerable()
        {
            foreach (IVisual visual in _root.GetSelfAndVisualDescendants())
            {
                visual.InvalidateVisual();
            }
        }

        private struct InvalidateVisualVisitor : ITreeVisitor<IVisual>
        {
            public TreeVisit Visit(IVisual target)
            {
                target.InvalidateVisual();

                return TreeVisit.Continue;
            }
        }
    }
}

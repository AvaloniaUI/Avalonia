using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Controls
{
    [MemoryDiagnoser]
    public class TemplateApplicationBenchmarks
    {
        private IDisposable? _app;
        private TestRoot? _root;

        [Params(1, 5, 10)]
        public int TemplateComplexity { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
            _root = new TestRoot { Width = 1000, Height = 1000 };
            _root.LayoutManager.ExecuteInitialLayoutPass();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _root = null;
            _app?.Dispose();
        }

        /// <summary>
        /// Benchmark applying template to a simple control
        /// </summary>
        [Benchmark(Baseline = true)]
        public void ApplySimpleTemplate()
        {
            var control = new TemplatedControlWithSimpleTemplate(TemplateComplexity);
            _root!.Child = control;
            control.ApplyTemplate();
            _root.Child = null;
        }

        /// <summary>
        /// Benchmark applying template with nested TemplatedControls
        /// </summary>
        [Benchmark]
        public void ApplyNestedTemplate()
        {
            var control = new TemplatedControlWithNestedTemplate(TemplateComplexity);
            _root!.Child = control;
            control.ApplyTemplate();
            _root.Child = null;
        }

        /// <summary>
        /// Benchmark template reapplication (style change)
        /// </summary>
        [Benchmark]
        public void ReapplyTemplate()
        {
            var control = new TemplatedControlWithSimpleTemplate(TemplateComplexity);
            _root!.Child = control;
            control.ApplyTemplate();
            
            // Force template reapplication
            control.Template = CreateSimpleTemplate(TemplateComplexity);
            control.ApplyTemplate();
            
            _root.Child = null;
        }

        /// <summary>
        /// Benchmark creating multiple templated controls
        /// </summary>
        [Benchmark]
        public void CreateMultipleTemplatedControls()
        {
            var panel = new StackPanel();
            _root!.Child = panel;

            for (int i = 0; i < 10; i++)
            {
                var control = new TemplatedControlWithSimpleTemplate(TemplateComplexity);
                panel.Children.Add(control);
                control.ApplyTemplate();
            }

            panel.Children.Clear();
            _root.Child = null;
        }

        /// <summary>
        /// Benchmark applying Button template (real-world control)
        /// </summary>
        [Benchmark]
        public void ApplyButtonTemplate()
        {
            var button = new Button { Content = "Test" };
            _root!.Child = button;
            button.ApplyTemplate();
            _root.Child = null;
        }

        private static IControlTemplate CreateSimpleTemplate(int complexity)
        {
            return new FuncControlTemplate<TemplatedControl>((parent, scope) =>
            {
                var root = new Border { Name = "PART_Root" };
                scope.Register("PART_Root", root);

                Control current = root;
                for (int i = 0; i < complexity; i++)
                {
                    var child = new Border { Name = $"Border{i}" };
                    if (current is Border b)
                        b.Child = child;
                    current = child;
                }

                return root;
            });
        }

        private class TemplatedControlWithSimpleTemplate : TemplatedControl
        {
            public TemplatedControlWithSimpleTemplate(int complexity)
            {
                Template = CreateSimpleTemplate(complexity);
            }
        }

        private class TemplatedControlWithNestedTemplate : TemplatedControl
        {
            public TemplatedControlWithNestedTemplate(int depth)
            {
                Template = new FuncControlTemplate<TemplatedControl>((parent, scope) =>
                {
                    var root = new Border { Name = "PART_Root" };
                    scope.Register("PART_Root", root);

                    if (depth > 1)
                    {
                        var nested = new TemplatedControlWithNestedTemplate(depth - 1);
                        root.Child = nested;
                    }

                    return root;
                });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Logging;
using Avalonia.Reactive;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Markup.UnitTests.Data
{
    public class BindingTests_Logging
    {
        public class DataContext
        {
            [Fact]
            public void Should_Not_Log_Missing_Member_On_Null_DataContext()
            {
                var target = new Decorator { };
                var root = new TestRoot(target);
                var binding = new Binding("Foo");

                using (AssertNoLog())
                {
                    target.Bind(Control.TagProperty, binding);
                }
            }

            [Fact]
            public void Should_Log_Missing_Member_On_DataContext()
            {
                var target = new Decorator { DataContext = new TestClass("foo") };
                var root = new TestRoot(target);
                var binding = new Binding("Foo.Bar");

                using (AssertLog(
                    target,
                    binding.Path,
                    "Could not find a matching property accessor for 'Bar' on 'System.String'.",
                    "Bar"))
                {
                    target.Bind(Control.TagProperty, binding);
                }
            }

            [Fact]
            public void Should_Log_Null_In_Binding_Chain()
            {
                var target = new Decorator { DataContext = new TestClass() };
                var root = new TestRoot(target);
                var binding = new Binding("Foo.Length");

                using (AssertLog(target, binding.Path, "Value is null.", "Foo"))
                {
                    target.Bind(Control.TagProperty, binding);
                }
            }
        }

        public class Source
        {
            [Fact]
            public void Should_Log_Null_Source()
            {
                var target = new Decorator { };
                var root = new TestRoot(target);
                var binding = new Binding("Foo") { Source = null };

                using (AssertLog(target, binding.Path, "Binding Source is null.", "(source)"))
                {
                    target.Bind(Control.TagProperty, binding);
                }
            }

            [Fact]
            public void Should_Log_Null_Source_For_Unrooted_Control()
            {
                var target = new Decorator { };
                var binding = new Binding("Foo") { Source = null };

                using (AssertLog(target, binding.Path, "Binding Source is null.", "(source)"))
                {
                    target.Bind(Control.TagProperty, binding);
                }
            }
        }

        public class LogicalAncestor
        {
            [Fact]
            public void Should_Log_Ancestor_Not_Found()
            {
                var target = new Decorator { };
                var root = new TestRoot(target);
                var binding = new Binding("$parent[TextBlock]") { TypeResolver = ResolveType };

                using (AssertLog(target, binding.Path, "Ancestor not found.", "$parent[TextBlock]"))
                {
                    target.Bind(Control.TagProperty, binding);
                }
            }

            [Fact]
            public void Should_Not_Log_Ancestor_Not_Found_For_Unrooted_Control()
            {
                var target = new Decorator { };
                var binding = new Binding("$parent[TextBlock]") { TypeResolver = ResolveType };

                using (AssertNoLog())
                {
                    target.Bind(Control.TagProperty, binding);
                }
            }
        }

        public class VisualAncestor
        {
            [Fact]
            public void Should_Log_Ancestor_Not_Found()
            {
                var target = new Decorator { };
                var root = new TestRoot(target);
                var binding = new Binding 
                { 
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor)
                    {
                        AncestorType = typeof(TextBlock),
                    }
                };

                using (AssertLog(target, "$visualParent[TextBlock]", "Ancestor not found.", "$visualParent[TextBlock]"))
                {
                    target.Bind(Control.TagProperty, binding);
                }
            }

            [Fact]
            public void Should_Log_Ancestor_Property_Not_Found()
            {
                var target = new Decorator { };
                var root = new TestRoot(target);
                var binding = new Binding("Foo")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor)
                    {
                        AncestorType = typeof(TestRoot),
                    }
                };

                using (AssertLog(
                    target,
                    "$visualParent[TestRoot].Foo",
                    "Could not find a matching property accessor for 'Foo' on 'Avalonia.UnitTests.TestRoot'.",
                    "Foo"))
                {
                    target.Bind(Control.TagProperty, binding);
                }
            }

            [Fact]
            public void Should_Not_Log_Ancestor_Not_Found_For_Unrooted_Control()
            {
                var target = new Decorator { };
                var binding = new Binding
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor)
                    {
                        AncestorType = typeof(Window),
                    }
                };

                using (AssertNoLog())
                {
                    target.Bind(Control.TagProperty, binding);
                }
            }
        }

        public class Fallback
        {
            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public void Should_Log_Invalid_FallbackValue(bool rooted)
            {
                var target = new Decorator { };
                var binding = new Binding("foo") { FallbackValue = "bar" };

                if (rooted)
                    new TestRoot(target);

                // An invalid fallback value is invalid whether the control is rooted or not.
                using (AssertLog(
                    target, 
                    binding.Path, 
                    "Could not convert FallbackValue 'bar' to 'System.Double'.",
                    level: LogEventLevel.Error,
                    property: Visual.OpacityProperty))
                {
                    target.Bind(Visual.OpacityProperty, binding);
                }
            }

            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public void Should_Log_Invalid_TargetNullValue(bool rooted)
            {
                var target = new Decorator { };
                var binding = new Binding() { TargetNullValue = "foo" };

                if (rooted)
                    new TestRoot(target);

                // An invalid target null value is invalid whether the control is rooted or not.
                using (AssertLog(
                    target,
                    "",
                    "Could not convert TargetNullValue 'foo' to 'System.Double'.",
                    level: LogEventLevel.Error,
                    property: Visual.OpacityProperty))
                {
                    target.Bind(Visual.OpacityProperty, binding);
                }
            }
        }

        public class NonControlDataContext
        {
            [Fact]
            public void Should_Not_Log_Missing_Member_On_Null_DataContext()
            {
                var target = new TestRoot();
                var binding = new Binding("Foo") { DefaultAnchor = new(target) };

                target.KeyBindings.Add(new KeyBinding
                {
                    Gesture = new KeyGesture(Key.A),
                    [!KeyBinding.CommandProperty] = binding
                });

                using (AssertNoLog())
                {
                    target.Bind(Control.TagProperty, binding);
                }
            }

            [Fact]
            public void Should_Log_Missing_Member_On_DataContext()
            {
                var target = new TestRoot();
                var binding = new Binding("Foo") { DefaultAnchor = new(target) };

                target.KeyBindings.Add(new KeyBinding
                {
                    Gesture = new KeyGesture(Key.A),
                    [!KeyBinding.CommandProperty] = binding
                });

                target.DataContext = new object();

                using (AssertLog(
                    target,
                    binding.Path,
                    "Could not find a matching property accessor for 'Foo' on 'System.Object'.",
                    "Foo"))
                {
                    target.Bind(Control.TagProperty, binding);
                }
            }
        }

        private static IDisposable AssertLog(
            AvaloniaObject target, 
            string expression,
            string message,
            string? errorPoint = null,
            LogEventLevel level = LogEventLevel.Warning,
            AvaloniaProperty? property = null)
        {
            var logs = new List<LogMessage>();
            var sink = TestLogSink.Start((l, a, s, m, p) =>
            {
                if (l >= level)
                    logs.Add(new(l, a, s, m, p));
            });

            return Disposable.Create(() =>
            {
                sink.Dispose();
                Assert.Equal(1, logs.Count);

                var l = logs[0];
                var messageTemplate = errorPoint is not null ?
                    "An error occurred binding {Property} to {Expression} at {ExpressionErrorPoint}: {Message}" :
                    "An error occurred binding {Property} to {Expression}: {Message}";

                Assert.Equal(level, l.level);
                Assert.Equal(LogArea.Binding, l.area);
                Assert.Equal(target, l.source);
                Assert.Equal(messageTemplate, l.messageTemplate);
                Assert.Equal(property ?? Control.TagProperty, l.propertyValues[0]);
                Assert.Equal(expression, l.propertyValues[1]);

                if (errorPoint is not null)
                {
                    Assert.Equal(errorPoint, l.propertyValues[2]);
                    Assert.Equal(message, l.propertyValues[3]);
                }
                else
                {
                    Assert.Equal(message, l.propertyValues[2]);
                }
            });
        }

        private static IDisposable AssertNoLog()
        {
            var count = 0;
            var sink = TestLogSink.Start((l, a, s, m, p) =>
            {
                if (l >= LogEventLevel.Warning)
                    ++count;
            });

            return Disposable.Create(() =>
            {
                sink.Dispose();
                Assert.Equal(0, count);
            });
        }

        private static Type ResolveType(string? ns, string typeName)
        {
            return typeName switch
            {
                "TextBlock" => typeof(TextBlock),
                "TestRoot" => typeof(TestRoot),
                _ => throw new InvalidOperationException($"Could not resolve type {typeName}.")
            };
        }

        private class TestClass
        {
            public TestClass(string? foo = null) => Foo = foo;
            public string? Foo { get; set; }
        }

        private record LogMessage(LogEventLevel level, string area, object source, string messageTemplate, params object[] propertyValues);
    }
}

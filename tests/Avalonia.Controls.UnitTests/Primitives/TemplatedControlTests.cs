// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class TemplatedControlTests
    {
        [Fact]
        public void Template_Doesnt_Get_Executed_On_Set()
        {
            bool executed = false;

            var template = new FuncControlTemplate((_, __) =>
            {
                executed = true;
                return new Control();
            });

            var target = new TemplatedControl
            {
                Template = template,
            };

            Assert.False(executed);
        }

        [Fact]
        public void Template_Gets_Executed_On_Measure()
        {
            bool executed = false;

            var template = new FuncControlTemplate((_, __) =>
            {
                executed = true;
                return new Control();
            });

            var target = new TemplatedControl
            {
                Template = template,
            };

            target.Measure(new Size(100, 100));

            Assert.True(executed);
        }

        [Fact]
        public void ApplyTemplate_Should_Create_Visual_Children()
        {
            var target = new TemplatedControl
            {
                Template = new FuncControlTemplate((_, __) => new Decorator
                {
                    Child = new Panel
                    {
                        Children =
                        {
                            new TextBlock(),
                            new Border(),
                        }
                    }
                }),
            };

            target.ApplyTemplate();

            var types = target.GetVisualDescendants().Select(x => x.GetType()).ToList();

            Assert.Equal(
                new[]
                {
                    typeof(Decorator),
                    typeof(Panel),
                    typeof(TextBlock),
                    typeof(Border)
                },
                types);
            Assert.Empty(target.GetLogicalChildren());
        }

        [Fact]
        public void Templated_Children_Should_Have_TemplatedParent_Set()
        {
            var target = new TemplatedControl
            {
                Template = new FuncControlTemplate((_, __) => new Decorator
                {
                    Child = new Panel
                    {
                        Children =
                        {
                            new TextBlock(),
                            new Border(),
                        }
                    }
                }),
            };

            target.ApplyTemplate();

            var templatedParents = target.GetVisualDescendants()
                .OfType<IControl>()
                .Select(x => x.TemplatedParent)
                .ToList();

            Assert.Equal(4, templatedParents.Count);
            Assert.True(templatedParents.All(x => x == target));
        }

        [Fact]
        public void Templated_Child_Should_Have_Parent_Set()
        {
            var target = new TemplatedControl
            {
                Template = new FuncControlTemplate((_, __) => new Decorator())
            };

            target.ApplyTemplate();

            var child = (Decorator)target.GetVisualChildren().Single();

            Assert.Equal(target, child.Parent);
            Assert.Equal(target, child.GetLogicalParent());
        }

        [Fact]
        public void Changing_Template_Should_Clear_Old_Templated_Childs_Parent()
        {
            var target = new TemplatedControl
            {
                Template = new FuncControlTemplate((_, __) => new Decorator())
            };

            target.ApplyTemplate();

            var child = (Decorator)target.GetVisualChildren().Single();

            target.Template = new FuncControlTemplate((_, __) => new Canvas());
            target.ApplyTemplate();

            Assert.Null(child.Parent);
        }

        [Fact]
        public void Nested_Templated_Control_Should_Not_Have_Template_Applied()
        {
            var target = new TemplatedControl
            {
                Template = new FuncControlTemplate((_, __) => new ScrollViewer())
            };

            target.ApplyTemplate();

            var child = (ScrollViewer)target.GetVisualChildren().Single();
            Assert.Empty(child.GetVisualChildren());
        }

        [Fact]
        public void Templated_Children_Should_Be_Styled()
        {
            using (UnitTestApplication.Start(TestServices.MockStyler))
            {
                TestTemplatedControl target;

                var root = new TestRoot
                {
                    Child = target = new TestTemplatedControl
                    {
                        Template = new FuncControlTemplate((_, __) =>
                        {
                            return new StackPanel
                            {
                                Children =
                                {
                                    new TextBlock
                                    {
                                    }
                                }
                            };
                        }),
                    }
                };

                target.ApplyTemplate();

                var styler = Mock.Get(UnitTestApplication.Current.Services.Styler);
                styler.Verify(x => x.ApplyStyles(It.IsAny<TestTemplatedControl>()), Times.Once());
                styler.Verify(x => x.ApplyStyles(It.IsAny<StackPanel>()), Times.Once());
                styler.Verify(x => x.ApplyStyles(It.IsAny<TextBlock>()), Times.Once());
            }
        }

        [Fact]
        public void Nested_Templated_Controls_Have_Correct_TemplatedParent()
        {
            var target = new TestTemplatedControl
            {
                Template = new FuncControlTemplate((_, __) =>
                {
                    return new ContentControl
                    {
                        Template = new FuncControlTemplate((parent, ___) =>
                        {
                            return new Border
                            {
                                Child = new ContentPresenter
                                {
                                    [~ContentPresenter.ContentProperty] = parent.GetObservable(ContentControl.ContentProperty).ToBinding(),
                                }
                            };
                        }),
                        Content = new Decorator
                        {
                            Child = new TextBlock()
                        }
                    };
                }),
            };

            target.ApplyTemplate();

            var contentControl = target.GetTemplateChildren().OfType<ContentControl>().Single();
            contentControl.ApplyTemplate();

            var border = contentControl.GetTemplateChildren().OfType<Border>().Single();
            var presenter = contentControl.GetTemplateChildren().OfType<ContentPresenter>().Single();
            var decorator = (Decorator)presenter.Content;
            var textBlock = (TextBlock)decorator.Child;

            Assert.Equal(target, contentControl.TemplatedParent);
            Assert.Equal(contentControl, border.TemplatedParent);
            Assert.Equal(contentControl, presenter.TemplatedParent);
            Assert.Equal(target, decorator.TemplatedParent);
            Assert.Equal(target, textBlock.TemplatedParent);
        }

        [Fact]
        public void ApplyTemplate_Should_Raise_TemplateApplied()
        {
            var target = new TestTemplatedControl
            {
                Template = new FuncControlTemplate((_, __) => new Decorator())
            };

            var raised = false;

            target.TemplateApplied += (s, e) =>
            {
                Assert.Equal(TemplatedControl.TemplateAppliedEvent, e.RoutedEvent);
                Assert.Same(target, e.Source);
                Assert.NotNull(e.NameScope);
                raised = true;
            };

            target.ApplyTemplate();

            Assert.True(raised);
        }

        [Fact]
        public void Applying_New_Template_Clears_TemplatedParent_Of_Old_Template_Children()
        {
            var target = new TestTemplatedControl
            {
                Template = new FuncControlTemplate((_, __) => new Decorator
                {
                    Child = new Border(),
                })
            };

            target.ApplyTemplate();

            var decorator = (Decorator)target.GetVisualChildren().Single();
            var border = (Border)decorator.Child;

            Assert.Equal(target, decorator.TemplatedParent);
            Assert.Equal(target, border.TemplatedParent);

            target.Template = new FuncControlTemplate((_, __) => new Canvas());

            // Templated children should not be removed here: the control may be re-added
            // somewhere with the same template, so they could still be of use.
            Assert.Same(decorator, target.GetVisualChildren().Single());
            Assert.Equal(target, decorator.TemplatedParent);
            Assert.Equal(target, border.TemplatedParent);

            target.ApplyTemplate();

            Assert.Null(decorator.TemplatedParent);
            Assert.Null(border.TemplatedParent);
        }

        [Fact]
        public void TemplateChild_AttachedToLogicalTree_Should_Be_Raised()
        {
            Border templateChild = new Border();
            var root = new TestRoot
            {
                Child = new TestTemplatedControl
                {
                    Template = new FuncControlTemplate((_, __) => new Decorator
                    {
                        Child = templateChild,
                    })
                }
            };

            var raised = false;
            templateChild.AttachedToLogicalTree += (s, e) => raised = true;

            root.Child.ApplyTemplate();
            Assert.True(raised);
        }

        [Fact]
        public void TemplateChild_DetachedFromLogicalTree_Should_Be_Raised()
        {
            Border templateChild = new Border();
            var root = new TestRoot
            {
                Child = new TestTemplatedControl
                {
                    Template = new FuncControlTemplate((_, __) => new Decorator
                    {
                        Child = templateChild,
                    })
                }
            };

            root.Child.ApplyTemplate();

            var raised = false;
            templateChild.DetachedFromLogicalTree += (s, e) => raised = true;

            root.Child = null;
            Assert.True(raised);
        }

        [Fact]
        public void Removing_From_LogicalTree_Should_Not_Remove_Child()
        {
            using (UnitTestApplication.Start(TestServices.RealStyler))
            {
                Border templateChild = new Border();
                TestTemplatedControl target;
                var root = new TestRoot
                {
                    Styles =
                    {
                        new Style(x => x.OfType<TestTemplatedControl>())
                        {
                            Setters = new[]
                            {
                                new Setter(
                                    TemplatedControl.TemplateProperty,
                                    new FuncControlTemplate((_, __) => new Decorator
                                    {
                                        Child = new Border(),
                                    }))
                            }
                        }
                    },
                    Child = target = new TestTemplatedControl()
                };

                Assert.NotNull(target.Template);
                target.ApplyTemplate();

                root.Child = null;

                Assert.Null(target.Template);
                Assert.IsType<Decorator>(target.GetVisualChildren().Single());
            }
        }

        [Fact]
        public void Re_adding_To_Same_LogicalTree_Should_Not_Recreate_Template()
        {
            using (UnitTestApplication.Start(TestServices.RealStyler))
            {
                TestTemplatedControl target;
                var root = new TestRoot
                {
                    Styles =
                    {
                        new Style(x => x.OfType<TestTemplatedControl>())
                        {
                            Setters = new[]
                            {
                                new Setter(
                                    TemplatedControl.TemplateProperty,
                                    new FuncControlTemplate((_, __) => new Decorator
                                    {
                                        Child = new Border(),
                                    }))
                            }
                        }
                    },
                    Child = target = new TestTemplatedControl()
                };

                Assert.NotNull(target.Template);
                target.ApplyTemplate();
                var expected = (Decorator)target.GetVisualChildren().Single();

                root.Child = null;
                root.Child = target;
                target.ApplyTemplate();

                Assert.Same(expected, target.GetVisualChildren().Single());
            }
        }

        [Fact]
        public void Re_adding_To_Different_LogicalTree_Should_Recreate_Template()
        {
            using (UnitTestApplication.Start(TestServices.RealStyler))
            {
                TestTemplatedControl target;

                var root = new TestRoot
                {
                    Styles =
                    {
                        new Style(x => x.OfType<TestTemplatedControl>())
                        {
                            Setters = new[]
                            {
                                new Setter(
                                    TemplatedControl.TemplateProperty,
                                    new FuncControlTemplate((_, __) => new Decorator
                                    {
                                        Child = new Border(),
                                    }))
                            }
                        }
                    },
                    Child = target = new TestTemplatedControl()
                };

                var root2 = new TestRoot
                {
                    Styles =
                    {
                        new Style(x => x.OfType<TestTemplatedControl>())
                        {
                            Setters = new[]
                            {
                                new Setter(
                                    TemplatedControl.TemplateProperty,
                                    new FuncControlTemplate((_, __) => new Decorator
                                    {
                                        Child = new Border(),
                                    }))
                            }
                        }
                    },
                };

                Assert.NotNull(target.Template);
                target.ApplyTemplate();

                var expected = (Decorator)target.GetVisualChildren().Single();

                root.Child = null;
                root2.Child = target;
                target.ApplyTemplate();

                var child = target.GetVisualChildren().Single();
                Assert.NotNull(target.Template);
                Assert.NotNull(child);
                Assert.NotSame(expected, child);
            }
        }

        [Fact]
        public void Moving_To_New_LogicalTree_Should_Detach_Attach_Template_Child()
        {
            using (UnitTestApplication.Start(TestServices.RealStyler))
            {
                TestTemplatedControl target;
                var root = new TestRoot
                {
                    Child = target = new TestTemplatedControl
                    {
                        Template = new FuncControlTemplate((_, __) => new Decorator()),
                    }
                };

                Assert.NotNull(target.Template);
                target.ApplyTemplate();

                var templateChild = (ILogical)target.GetVisualChildren().Single();
                Assert.True(templateChild.IsAttachedToLogicalTree);

                root.Child = null;
                Assert.False(templateChild.IsAttachedToLogicalTree);

                var newRoot = new TestRoot { Child = target };
                Assert.True(templateChild.IsAttachedToLogicalTree);
            }
        }

        private static IControl ScrollingContentControlTemplate(ContentControl control, INameScope scope)
        {
            return new Border
            {
                Child = new ScrollViewer
                {
                    Template = new FuncControlTemplate<ScrollViewer>(ScrollViewerTemplate),
                    Name = "ScrollViewer",
                    Content = new ContentPresenter
                    {
                        Name = "PART_ContentPresenter",
                        [!ContentPresenter.ContentProperty] = control[!ContentControl.ContentProperty],
                    }.RegisterInNameScope(scope)
                }.RegisterInNameScope(scope)
            };
        }

        private static Control ScrollViewerTemplate(ScrollViewer control, INameScope scope)
        {
            var result = new ScrollContentPresenter
            {
                Name = "PART_ContentPresenter",
                [~ContentPresenter.ContentProperty] = control[~ContentControl.ContentProperty],
            }.RegisterInNameScope(scope);

            return result;
        }
    }
}

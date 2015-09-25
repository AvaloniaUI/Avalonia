// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Moq;
using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Styling;
using Perspex.VisualTree;
using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class TemplatedControlTests
    {
        [Fact]
        public void Template_Doesnt_Get_Executed_On_Set()
        {
            bool executed = false;

            var template = new ControlTemplate(_ =>
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

            var template = new ControlTemplate(_ =>
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
        public void Template_Result_Becomes_Visual_Child()
        {
            Control templateResult = new Control();

            var template = new ControlTemplate(_ =>
            {
                return templateResult;
            });

            var target = new TemplatedControl
            {
                Template = template,
            };

            target.Measure(new Size(100, 100));
            var children = target.GetVisualChildren().ToList();

            Assert.Equal(new[] { templateResult }, children);
        }

        [Fact]
        public void TemplatedParent_Is_Set_On_Generated_Template()
        {
            Control templateResult = new Control();

            var template = new ControlTemplate(_ =>
            {
                return templateResult;
            });

            var target = new TemplatedControl
            {
                Template = template,
            };

            target.Measure(new Size(100, 100));

            Assert.Equal(target, templateResult.TemplatedParent);
        }

        [Fact]
        public void OnTemplateApplied_Is_Called()
        {
            var target = new TestTemplatedControl
            {
                Template = new ControlTemplate(_ =>
                {
                    return new Control();
                })
            };

            target.Measure(new Size(100, 100));

            Assert.True(target.OnTemplateAppliedCalled);
        }

        [Fact]
        public void Template_Should_Be_Instantated()
        {
            var target = new TestTemplatedControl
            {
                Template = new ControlTemplate(_ =>
                {
                    return new StackPanel
                    {
                        Children = new Controls
                        {
                            new TextBlock
                            {
                            }
                        }
                    };
                }),
            };

            target.ApplyTemplate();

            var child = target.GetVisualChildren().Single();
            Assert.IsType<StackPanel>(child);
            child = child.VisualChildren.Single();
            Assert.IsType<TextBlock>(child);
        }

        [Fact]
        public void Templated_Children_Should_Be_Styled()
        {
            using (PerspexLocator.EnterScope())
            {
                var styler = new Mock<IStyler>();

                PerspexLocator.CurrentMutable.Bind<IStyler>().ToConstant(styler.Object);

                TestTemplatedControl target;

                var root = new TestRoot
                {
                    Child = target = new TestTemplatedControl
                    {
                        Template = new ControlTemplate(_ =>
                        {
                            return new StackPanel
                            {
                                Children = new Controls
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

                styler.Verify(x => x.ApplyStyles(It.IsAny<TestTemplatedControl>()), Times.Once());
                styler.Verify(x => x.ApplyStyles(It.IsAny<StackPanel>()), Times.Once());
                styler.Verify(x => x.ApplyStyles(It.IsAny<TextBlock>()), Times.Once());
            }
        }

        [Fact]
        public void Templated_Children_Should_Have_TemplatedParent_Set()
        {
            var target = new TestTemplatedControl
            {
                Template = new ControlTemplate(_ =>
                {
                    return new StackPanel
                    {
                        Children = new Controls
                        {
                            new TextBlock
                            {
                            }
                        }
                    };
                }),
            };

            target.ApplyTemplate();

            var panel = target.GetTemplateChildren().OfType<StackPanel>().Single();
            var textBlock = target.GetTemplateChildren().OfType<TextBlock>().Single();

            Assert.Equal(target, panel.TemplatedParent);
            Assert.Equal(target, textBlock.TemplatedParent);
        }

        [Fact]
        public void Presenter_Children_Should_Not_Have_TemplatedParent_Set()
        {
            var target = new TestTemplatedControl
            {
                Template = new ControlTemplate(_ =>
                {
                    return new ContentPresenter
                    {
                        Content = new TextBlock
                        {
                        }
                    };
                }),
            };

            target.ApplyTemplate();

            var presenter = target.GetTemplateChildren().OfType<ContentPresenter>().Single();
            var textBlock = (TextBlock)presenter.Child;

            Assert.Equal(target, presenter.TemplatedParent);
            Assert.Null(textBlock.TemplatedParent);
        }

        [Fact]
        public void Nested_Templated_Controls_Have_Correct_TemplatedParent()
        {
            var target = new TestTemplatedControl
            {
                Template = new ControlTemplate(_ =>
                {
                    return new ContentControl
                    {
                        Template = new ControlTemplate(parent =>
                        {
                            return new Border
                            {
                                Child = new ContentPresenter
                                {
                                    [~ContentPresenter.ContentProperty] = parent.GetObservable(ContentControl.ContentProperty),
                                }
                            };
                        }),
                        Content = new TextBlock
                        {
                        }
                    };
                }),
            };

            target.ApplyTemplate();

            var contentControl = target.GetTemplateChildren().OfType<ContentControl>().Single();
            var border = contentControl.GetTemplateChildren().OfType<Border>().Single();
            var presenter = contentControl.GetTemplateChildren().OfType<ContentPresenter>().Single();
            var textBlock = (TextBlock)presenter.Content;

            Assert.Equal(target, contentControl.TemplatedParent);
            Assert.Equal(contentControl, border.TemplatedParent);
            Assert.Equal(contentControl, presenter.TemplatedParent);
            Assert.Equal(target, textBlock.TemplatedParent);
        }
    }
}

// -----------------------------------------------------------------------
// <copyright file="TemplatedControlTests.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace Perspex.Controls.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Styling;
    using Perspex.VisualTree;
    using Splat;

    [TestClass]
    public class TemplatedControlTests
    {
        [TestMethod]
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

            Assert.IsFalse(executed);
        }

        [TestMethod]
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

            Assert.IsTrue(executed);
        }

        [TestMethod]
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

            CollectionAssert.AreEqual(new[] { templateResult }, children);
        }

        [TestMethod]
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

            Assert.AreEqual(target, templateResult.TemplatedParent);
        }

        [TestMethod]
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

            Assert.IsTrue(target.OnTemplateAppliedCalled);
        }

        [TestMethod]
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
            Assert.IsInstanceOfType(child, typeof(StackPanel));
            child = child.VisualChildren.Single();
            Assert.IsInstanceOfType(child, typeof(TextBlock));
        }

        [TestMethod]
        public void Templated_Children_Should_Be_Styled()
        {
            using (Locator.Current.WithResolver())
            {
                var styler = new Mock<IStyler>();
                Locator.CurrentMutable.Register(() => styler.Object, typeof(IStyler));

                TestTemplatedControl target;

                var root = new TestRoot
                {
                    Content = (target = new TestTemplatedControl
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
                    })
                };


                target.ApplyTemplate();

                styler.Verify(x => x.ApplyStyles(It.IsAny<TestTemplatedControl>()), Times.Once());
                styler.Verify(x => x.ApplyStyles(It.IsAny<StackPanel>()), Times.Once());
                styler.Verify(x => x.ApplyStyles(It.IsAny<TextBlock>()), Times.Once());
            }
        }

        [TestMethod]
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

            var panel = target.GetTemplateControls().OfType<StackPanel>().Single();
            var textBlock = target.GetTemplateControls().OfType<TextBlock>().Single();

            Assert.AreEqual(target, panel.TemplatedParent);
            Assert.AreEqual(target, textBlock.TemplatedParent);
        }

        [TestMethod]
        public void Presenter_Children_Should_Not_Have_TemplatedParent_Set()
        {
            var target = new TestTemplatedControl
            {
                Template = new ControlTemplate(_ =>
                {
                    return new ContentPresenter
                    {
                        Content =  new TextBlock
                        {
                        }
                    };
                }),
            };

            target.ApplyTemplate();

            var presenter = target.GetTemplateControls().OfType<ContentPresenter>().Single();
            var textBlock = (TextBlock)presenter.Child;

            Assert.AreEqual(target, presenter.TemplatedParent);
            Assert.IsNull(textBlock.TemplatedParent);
        }

        [TestMethod]
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
                                Content = new ContentPresenter
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

            var contentControl = target.GetTemplateControls().OfType<ContentControl>().Single();
            var border = contentControl.GetTemplateControls().OfType<Border>().Single();
            var presenter = contentControl.GetTemplateControls().OfType<ContentPresenter>().Single();
            var textBlock = (TextBlock)presenter.Content;

            Assert.AreEqual(target, contentControl.TemplatedParent);
            Assert.AreEqual(contentControl, border.TemplatedParent);
            Assert.AreEqual(contentControl, presenter.TemplatedParent);
            Assert.AreEqual(target, textBlock.TemplatedParent);
        }
    }
}


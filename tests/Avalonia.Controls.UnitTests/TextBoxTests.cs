// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Data;
using Avalonia.Platform;
using Avalonia.UnitTests;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextBoxTests
    {
        [Fact]
        public void DefaultBindingMode_Should_Be_TwoWay()
        {
            Assert.Equal(
                BindingMode.TwoWay,
                TextBox.TextProperty.GetMetadata(typeof(TextBox)).DefaultBindingMode);
        }

        [Fact]
        public void Typing_Beginning_With_0_Should_Not_Modify_Text_When_Bound_To_Int()
        {
            using (UnitTestApplication.Start(Services))
            {
                var source = new Class1();
                var target = new TextBox
                {
                    DataContext = source,
                    Template = CreateTemplate(),
                };

                target.ApplyTemplate();
                target.Bind(TextBox.TextProperty, new Binding(nameof(Class1.Foo), BindingMode.TwoWay));

                Assert.Equal("0", target.Text);

                target.CaretIndex = 1;
                target.RaiseEvent(new TextInputEventArgs
                {
                    RoutedEvent = InputElement.TextInputEvent,
                    Text = "2",
                });

                Assert.Equal("02", target.Text);
            }
        }

        private static TestServices Services => TestServices.MockThreadingInterface.With(
            standardCursorFactory: Mock.Of<IStandardCursorFactory>());

        private IControlTemplate CreateTemplate()
        {
            return new FuncControlTemplate<TextBox>(control =>
                new TextPresenter
                {
                    Name = "PART_TextPresenter",
                    [!!TextPresenter.TextProperty] = new Binding
                    {
                        Path = "Text",
                        Mode = BindingMode.TwoWay,
                        Priority = BindingPriority.TemplatedParent,
                        RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                    },
                });
        }

        public void Control_Backspace_Should_Remove_The_Word_Before_The_Caret_If_There_Is_No_Selection()
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformThreadingInterface>()
                .ToConstant(TestServices.MockThreadingInterface.ThreadingInterface);

            TextBox textBox = new TextBox
            {
                Text = "First Second Third Fourth",
                CaretIndex = 5
            };

            // (First| Second Third Fourth)
            RaiseKeyEvent(textBox, Key.Back, InputModifiers.Control);
            Assert.Equal(" Second Third Fourth", textBox.Text);

            // ( Second |Third Fourth)
            textBox.CaretIndex = 8;
            RaiseKeyEvent(textBox, Key.Back, InputModifiers.Control);
            Assert.Equal(" Third Fourth", textBox.Text);

            // ( Thi|rd Fourth)
            textBox.CaretIndex = 4;
            RaiseKeyEvent(textBox, Key.Back, InputModifiers.Control);
            Assert.Equal(" rd Fourth", textBox.Text);

            // ( rd F[ou]rth)
            textBox.SelectionStart = 5;
            textBox.SelectionEnd = 7;

            RaiseKeyEvent(textBox, Key.Back, InputModifiers.Control);
            Assert.Equal(" rd Frth", textBox.Text);

            // ( |rd Frth)
            textBox.CaretIndex = 1;
            RaiseKeyEvent(textBox, Key.Back, InputModifiers.Control);
            Assert.Equal("rd Frth", textBox.Text);
        }

        [Fact]
        public void Control_Delete_Should_Remove_The_Word_After_The_Caret_If_There_Is_No_Selection()
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformThreadingInterface>()
                .ToConstant(TestServices.MockThreadingInterface.ThreadingInterface);

            TextBox textBox = new TextBox
            {
                Text = "First Second Third Fourth",
                CaretIndex = 19
            };

            // (First Second Third |Fourth)
            RaiseKeyEvent(textBox, Key.Delete, InputModifiers.Control);
            Assert.Equal("First Second Third ", textBox.Text);

            // (First Second| Third )
            textBox.CaretIndex = 12;
            RaiseKeyEvent(textBox, Key.Delete, InputModifiers.Control);
            Assert.Equal("First Second ", textBox.Text);

            // (First Sec|ond )
            textBox.CaretIndex = 9;
            RaiseKeyEvent(textBox, Key.Delete, InputModifiers.Control);
            Assert.Equal("First Sec ", textBox.Text);

            // (Fi[rs]t Sec )
            textBox.SelectionStart = 2;
            textBox.SelectionEnd = 4;

            RaiseKeyEvent(textBox, Key.Delete, InputModifiers.Control);
            Assert.Equal("Fit Sec ", textBox.Text);

            // (Fit Sec| )
            textBox.CaretIndex = 7;
            RaiseKeyEvent(textBox, Key.Delete, InputModifiers.Control);
            Assert.Equal("Fit Sec", textBox.Text);
        }

        private void RaiseKeyEvent(TextBox textBox, Key key, InputModifiers inputModifiers)
        {
            textBox.RaiseEvent(new KeyEventArgs
            {
                RoutedEvent = InputElement.KeyDownEvent,
                Modifiers = inputModifiers,
                Key = key
            });
        }

        private class Class1 : NotifyingBase
        {
            private int _foo;

            public int Foo
            {
                get { return _foo; }
                set { _foo = value; RaisePropertyChanged(); }
            }
        }
    }
}

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Xunit;

namespace Avalonia.Markup.UnitTests.Data
{
    public class BindingTests_Method
    {
        [Fact]
        public void Binding_To_Private_Methods_Shouldnt_Work()
        {
            var vm = new TestClass();
            var target = new Button
            {
                DataContext = vm,
                [!Button.CommandProperty] = new Binding("MyMethod"),
            };
            target.RaiseEvent(new RoutedEventArgs(AccessKeyHandler.AccessKeyPressedEvent));

            Assert.False(vm.IsSet);
        }


        class TestClass
        {
            public bool IsSet { get; set; }
            private void MyMethod() => IsSet = true;
        }
    }
}

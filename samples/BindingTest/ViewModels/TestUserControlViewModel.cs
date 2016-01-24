using ReactiveUI;

namespace BindingTest.ViewModels
{
    public class TestUserControlViewModel : ReactiveObject
    {
        public string Content { get; } = "User Control Content";
    }
}

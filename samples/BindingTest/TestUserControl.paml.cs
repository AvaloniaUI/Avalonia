using Perspex.Controls;
using Perspex.Markup.Xaml;

namespace BindingTest
{
    public class TestUserControl : UserControl
    {
        public TestUserControl()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}

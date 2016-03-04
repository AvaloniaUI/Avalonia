using Perspex.Controls;
using Perspex.Markup.Xaml;

namespace BindingTest
{
    public class TestItemView : UserControl
    {
        public TestItemView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            PerspexXamlLoader.Load(this);
        }
    }
}

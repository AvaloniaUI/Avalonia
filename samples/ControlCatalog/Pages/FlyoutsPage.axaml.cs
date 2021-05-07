using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public class FlyoutsPage : UserControl
    {
        public FlyoutsPage()
        {
            InitializeComponent();

            var afp = this.FindControl<Panel>("AttachedFlyoutPanel");
            if (afp != null)
            {
                afp.DoubleTapped += Afp_DoubleTapped;
            }

            SetXamlTexts();
        }

        private void Afp_DoubleTapped(object sender, RoutedEventArgs e)
        {
            if (sender is Panel p)
            {
                FlyoutBase.ShowAttachedFlyout(p);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SetXamlTexts()
        {
            var bfxt = this.FindControl<TextBlock>("ButtonFlyoutXamlText");
            bfxt.Text = "<Button Content=\"Click me!\">\n" +
                        "    <Button.Flyout>\n" +
                        "        <Flyout>\n" +
                        "            <Panel Width=\"100\" Height=\"100\">\n" +
                        "                <TextBlock Text=\"Flyout Content!\" />\n" +
                        "            </Panel>\n" +
                        "        </Flyout>\n" +
                        "    </Button.Flyout>\n</Button>";

            var mfxt = this.FindControl<TextBlock>("MenuFlyoutXamlText");
            mfxt.Text = "<Button Content=\"Click me!\">\n" +
                    "    <Button.Flyout>\n" +
                    "        <MenuFlyout>\n" +
                    "            <MenuItem Header=\"Item 1\">\n" +
                    "            <MenuItem Header=\"Item 2\">\n" +
                    "        </MenuFlyout>\n" +
                    "    </Button.Flyout>\n</Button>";

            var afxt = this.FindControl<TextBlock>("AttachedFlyoutXamlText");
            afxt.Text = "<Panel Name=\"AttachedFlyoutPanel\">\n" +
                "    <FlyoutBase.AttachedFlyout>\n" +
                "        <Flyout>\n" +
                "            <Panel Height=\"100\">\n" +
                "                <TextBlock Text=\"Attached Flyout\" />\n" +
                "            </Panel>\n" +
                "        </Flyout>\n" +
                "    </FlyoutBase.AttachedFlyout>\n</Panel>" + 
                "\n\n In DoubleTapped handler:\n" +
                "FlyoutBase.ShowAttachedFlyout(AttachedFlyoutPanel);";

            var sfxt = this.FindControl<TextBlock>("SharedFlyoutXamlText");
            sfxt.Text = "Declare a flyout in Resources:\n" +
                "<Window.Resources>\n" +
                "    <Flyout x:Key=\"SharedFlyout\">\n" +
                "        <Panel Width=\"100\" Height=\"100\">\n" +
                "            <TextBlock Text=\"Flyout Content!\" />\n" +
                "        </Panel>\n" +
                "    </Flyout>\n</Window.Resources>\n\n" +
                "Then attach the flyout where you want it:\n" +
                "<Button Content=\"Launch Flyout here\" Flyout=\"{StaticResource SharedFlyout}\" />";
        }
    }
}

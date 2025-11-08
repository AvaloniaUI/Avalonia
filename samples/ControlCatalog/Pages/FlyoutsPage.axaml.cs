using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class FlyoutsPage : UserControl
    {
        public FlyoutsPage()
        {
            InitializeComponent();

            AttachedFlyoutPanel.DoubleTapped += Afp_DoubleTapped;

            SetXamlTexts();
        }

        private void Afp_DoubleTapped(object? sender, RoutedEventArgs e)
        {
            if (sender is Panel p)
            {
                FlyoutBase.ShowAttachedFlyout(p);
            }
        }

        private void SetXamlTexts()
        {
            var bfxt = ButtonFlyoutXamlText;
            bfxt.Text = "<Button Content=\"Click me!\">\n" +
                        "    <Button.Flyout>\n" +
                        "        <Flyout>\n" +
                        "            <Panel Width=\"100\" Height=\"100\">\n" +
                        "                <TextBlock Text=\"Flyout Content!\" />\n" +
                        "            </Panel>\n" +
                        "        </Flyout>\n" +
                        "    </Button.Flyout>\n</Button>";

            var mfxt = this.MenuFlyoutXamlText;
            mfxt.Text = "<Button Content=\"Click me!\">\n" +
                    "    <Button.Flyout>\n" +
                    "        <MenuFlyout>\n" +
                    "            <MenuItem Header=\"Item 1\">\n" +
                    "            <MenuItem Header=\"Item 2\">\n" +
                    "        </MenuFlyout>\n" +
                    "    </Button.Flyout>\n</Button>";

            var afxt = this.AttachedFlyoutXamlText;
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

            var sfxt = this.SharedFlyoutXamlText;
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

        public void CustomPlacementCallback(CustomPopupPlacement placement)
        {
            var r = new Random().Next();
            placement.Anchor = (r % 4) switch
            {
                1 => PopupAnchor.Top,
                2 => PopupAnchor.Left,
                3 => PopupAnchor.Right,
                _ => PopupAnchor.Bottom,
            };
            placement.Gravity = (r % 4) switch
            {
                1 => PopupGravity.Top,
                2 => PopupGravity.Left,
                3 => PopupGravity.Right,
                _ => PopupGravity.Bottom,
            };
            placement.Offset = new Point(r % 20, r % 20);
        }
    }
}

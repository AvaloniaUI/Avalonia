using Avalonia.Controls;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public partial class TabbedPageCustomTabBarPage : UserControl
    {
        private static readonly StreamGeometry HomeGeometry =
            StreamGeometry.Parse("M12.9942 2.79444C12.4118 2.30208 11.5882 2.30208 11.0058 2.79444L3.50582 9.39444C3.18607 9.66478 3 10.0634 3 10.4828V20.25C3 20.9404 3.55964 21.5 4.25 21.5H8.25C8.94036 21.5 9.5 20.9404 9.5 20.25V14.75C9.5 14.6119 9.61193 14.5 9.75 14.5H14.25C14.3881 14.5 14.5 14.6119 14.5 14.75V20.25C14.5 20.9404 15.0596 21.5 15.75 21.5H19.75C20.4404 21.5 21 20.9404 21 20.25V10.4828C21 10.0634 20.8139 9.66478 20.4942 9.39444L12.9942 2.79444Z");
        private static readonly StreamGeometry WalletGeometry =
            StreamGeometry.Parse("M4.25 4A2.25 2.25 0 002 6.25v11.5A2.25 2.25 0 004.25 20h15.5A2.25 2.25 0 0022 17.75v-8.5A2.25 2.25 0 0019.75 7H5.5a.75.75 0 010-1.5h14.25a.75.75 0 000-1.5H4.25zM16.5 14a1.25 1.25 0 100-2.5 1.25 1.25 0 000 2.5z");
        private static readonly StreamGeometry SendGeometry =
            StreamGeometry.Parse("M5.694 12l-1.612 7.066a.75.75 0 001.122.814L21.1 12.48a.75.75 0 000-1.28L5.204 3.8a.75.75 0 00-1.122.814L5.694 12zm.93-.75l6.626-.001a.75.75 0 010 1.5H6.624l-.001.001z");
        private static readonly StreamGeometry ActivityGeometry =
            StreamGeometry.Parse("M3 3.75A.75.75 0 013.75 3h.5a.75.75 0 01.75.75v7.5L8.293 8.543a.75.75 0 011.028-.014L12 11.02l3.72-4.647a.75.75 0 011.06-.1l3.47 2.776V3.75a.75.75 0 01.75-.75h.5a.75.75 0 01.75.75v16.5a.75.75 0 01-.75.75H3.75a.75.75 0 01-.75-.75V3.75z");
        private static readonly StreamGeometry ProfileGeometry =
            StreamGeometry.Parse("M12 2C9.243 2 7 4.243 7 7s2.243 5 5 5 5-2.243 5-5-2.243-5-5-5zM12 14c-5.523 0-10 3.582-10 8a1 1 0 001 1h18a1 1 0 001-1c0-4.418-4.477-8-10-8z");

        public TabbedPageCustomTabBarPage()
        {
            InitializeComponent();
            SetupIcons();
        }

        private void SetupIcons()
        {
            HomePage.Icon = new PathIcon { Data = HomeGeometry };
            WalletPage.Icon = new PathIcon { Data = WalletGeometry };
            SendPage.Icon = new PathIcon { Data = SendGeometry };
            ActivityPage.Icon = new PathIcon { Data = ActivityGeometry };
            ProfilePage.Icon = new PathIcon { Data = ProfileGeometry };
        }
    }
}

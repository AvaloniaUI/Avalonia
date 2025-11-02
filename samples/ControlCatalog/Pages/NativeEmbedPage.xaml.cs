using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace ControlCatalog.Pages
{
    public partial class NativeEmbedPage : UserControl
    {
        public NativeEmbedPage()
        {
            InitializeComponent();
        }

        public async void ShowPopupDelay(object sender, RoutedEventArgs args)
        {
            await Task.Delay(3000);
            ShowPopup(sender, args);
        }

        public void ShowPopup(object sender, RoutedEventArgs args)
        {
            new ContextMenu()
            {
                Items =
                {
                    new MenuItem() { Header = "Test" }, new MenuItem() { Header = "Test" }
                }
            }.Open((Control)sender);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == BoundsProperty)
            {
                var isMobile = change.GetNewValue<Rect>().Width < 1200;
                FirstPanel.Classes.Set("mobile", isMobile);
                SecondPanel.Classes.Set("mobile", isMobile);
            }
        }
    }

    public class EmbedSample : NativeControlHost
    {
        public static INativeDemoControl? Implementation { get; set; }

        static EmbedSample()
        {

        }

        public bool IsSecond { get; set; }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            return Implementation?.CreateControl(IsSecond, parent, () => base.CreateNativeControlCore(parent))
                ?? base.CreateNativeControlCore(parent);
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            base.DestroyNativeControlCore(control);
        }
    }

    public interface INativeDemoControl
    {
        /// <param name="isSecond">Used to specify which control should be displayed as a demo</param>
        /// <param name="parent"></param>
        /// <param name="createDefault"></param>
        IPlatformHandle CreateControl(bool isSecond, IPlatformHandle parent, Func<IPlatformHandle> createDefault);
    }
}

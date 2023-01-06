using System;
using System.Collections;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.VisualTree;
using CommunityToolkit.HighPerformance;
using ControlCatalog.Models;
using ControlCatalog.Pages;

namespace ControlCatalog
{
    public class MainView : UserControl
    {
        public MainView()
        {
            AvaloniaXamlLoader.Load(this);

            
        }

        [StructLayout(LayoutKind.Sequential)]
        struct FPixel
        {
            public Half R { get; set; }
            public Half G { get; set; }
            public Half B { get; set; }
            public Half A { get; set; }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            unsafe
            {
                base.OnAttachedToVisualTree(e);

                var image = this.Find<Image>("PART_Image");

                var bmp = new WriteableBitmap(new PixelSize(100, 100), new Vector(96, 96), PixelFormat.RgbaF16);

                using var framebuffer = bmp.Lock();

                var span = new Span<FPixel>(framebuffer.Address.ToPointer(), 100 * 100);
                
                for (int i = 0; i < (100 * 100); i++)
                {
                    span[i].R = (Half)1.0f;
                    span[i].A = (Half)1.0f;
                }

                image.Source = bmp;
            }
        }
    }
}

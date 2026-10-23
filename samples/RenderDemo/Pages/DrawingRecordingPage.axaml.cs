using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RenderDemo.Pages
{
    /// <summary>
    /// Shows the two ways a DrawingRecording is used from a control's Render
    /// method, side by side: an immutable recording replayed at many transforms
    /// per frame, and a compositor-bound recording whose mutable brush and pen
    /// animate through change tracking. Each side reports how often its Render
    /// method ran.
    /// </summary>
    public class DrawingRecordingPage : UserControl
    {
        public DrawingRecordingPage()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

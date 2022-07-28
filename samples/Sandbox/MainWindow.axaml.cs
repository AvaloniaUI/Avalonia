using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Sandbox
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();

            var textBox = this.FindControl<TextBox>("txtBox");

            textBox.TemplateApplied += TextBox_TemplateApplied;          
        }

        private void TextBox_TemplateApplied(object sender, Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
        {
            var textBox = sender as TextBox;

            var textPresenter = e.NameScope.Find("PART_TextPresenter") as TextPresenter;

            DataContext = new TestViewModel(textPresenter);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    public class TestViewModel : ViewModelBase
    {
        private readonly TextPresenter _textPresenter;
        private double _distance = 45;

        public TestViewModel(TextPresenter textPresenter)
        {
            _textPresenter = textPresenter;
        }

        public double Distance 
        { 
            get => _distance; 
            set
            {
                OnDistanceChanged(value);
                RaisePropertyChanged();
            }
        }

        private void OnDistanceChanged(double distance)
        {
            if(distance < 0)
            {
                distance = 0;
            }

            if(distance > _textPresenter.TextLayout.Bounds.Width)
            {
                distance = _textPresenter.TextLayout.Bounds.Width;
            }

            var height = _textPresenter.TextLayout.Bounds.Height;

            var distanceY = height / 2;

            _textPresenter.MoveCaretToPoint(new Point(distance, distanceY));

            var caretIndex = _textPresenter.CaretIndex;

            Debug.WriteLine(caretIndex);

            _distance = distance;
        }
    }

    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName]string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

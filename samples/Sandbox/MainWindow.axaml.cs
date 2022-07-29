using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Sandbox
{
    public class MainWindow : Window
    {
        private TestViewModel _dc;

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

            _dc = new TestViewModel(textPresenter);

            DataContext = _dc;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            _dc.InlineCollection = new InlineCollection
        {
            new Run(""),
            new Run("test3") {FontWeight = Avalonia.Media.FontWeight.Bold},
        };
            // _dc.Text = "nununu";
        }

        private void TextButton_OnClick(object? sender, RoutedEventArgs e)
        {
            _dc.Text = "nununu";
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

        private InlineCollection _inlineCollection;
        private string _text;

        public string Text
        {
            get => _text;
            set
            {
                _text = value;
                RaisePropertyChanged();
            }
        }

        public InlineCollection InlineCollection
        {
            get => _inlineCollection;
            set
            {
                _inlineCollection = value;
                RaisePropertyChanged();
            }
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

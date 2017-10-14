using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;

namespace ControlCatalog
{
    public class EditableTextBlock : TemplatedControl
    {
        private string _text;
        private TextBox _textBox;

        public static readonly DirectProperty<EditableTextBlock, string> TextProperty = TextBlock.TextProperty.AddOwner<EditableTextBlock>(
                o => o.Text,
                (o, v) => o.Text = v,
                defaultBindingMode: BindingMode.TwoWay,
                enableDataValidation: true);        

        [Content]
        public string Text
        {
            get { return _text; }
            set
            {
                SetAndRaise(TextProperty, ref _text, value);                
            }
        }

        public static readonly StyledProperty<bool> InEditModeProperty =
            AvaloniaProperty.Register<EditableTextBlock, bool>(nameof(InEditMode));

        public bool InEditMode
        {
            get { return GetValue(InEditModeProperty); }
            set { SetValue(InEditModeProperty, value); }
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);

            _textBox = e.NameScope.Find<TextBox>("PART_TextBox");

            _textBox.KeyUp += _textBox_KeyUp;            
        }

        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromLogicalTree(e);

            _textBox.KeyUp -= _textBox_KeyUp;
        }

        private void _textBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                ExitEditMode();
            }
        }

        private void EnterEditMode()
        {
            InEditMode = true;
            (VisualRoot as IInputRoot).MouseDevice.Capture(_textBox);
            _textBox.CaretIndex = Text.Length - 1;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                _textBox.Focus();
            });    
        }

        private void ExitEditMode()
        {
            InEditMode = false;
            (VisualRoot as IInputRoot).MouseDevice.Capture(null);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (!InEditMode)
            {
                if (e.MouseButton == MouseButton.Middle)
                {
                    EnterEditMode();
                }
                else if (e.ClickCount == 2)
                {
                    EnterEditMode();
                }
            }
            else
            {
                var hit = this.InputHitTest(e.GetPosition(this));

                if (!this.IsVisualAncestorOf(hit))
                {
                    ExitEditMode();
                }
            }
        }
    }
}

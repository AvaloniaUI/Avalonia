using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using System;

namespace Avalonia.Controls
{
    public class PasswordBox : TextBox, IStyleable
    {
        Type IStyleable.StyleKey => typeof(PasswordBox);

        public PasswordBox()
        {
            this.GetObservable(TextProperty).Subscribe(text =>
            {
                if (text != null)
                {
                    DisplayText = new string(PasswordChar, text.Length);
                }
                else
                {
                    DisplayText = null;
                }
            });
        }

        public static readonly StyledProperty<char> PasswordCharProperty = AvaloniaProperty.Register<PasswordBox, char>(nameof(PasswordChar), '*');

        public char PasswordChar
        {
            get => GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
        }

        public static readonly StyledProperty<string> DisplayTextProperty = AvaloniaProperty.Register<PasswordBox, string>(nameof(DisplayText));

        public string DisplayText
        {
            get => GetValue(DisplayTextProperty);
            set => SetValue(DisplayTextProperty, value);
        }

        protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);
        }
    }
}

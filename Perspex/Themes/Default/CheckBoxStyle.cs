// -----------------------------------------------------------------------
// <copyright file="ButtonStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Media;
    using Perspex.Styling;

    public class CheckBoxStyle : Styles
    {
        public CheckBoxStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<CheckBox>())
                {
                    Setters = new[]
                    {
                        new Setter(Button.TemplateProperty, ControlTemplate.Create<CheckBox>(this.Template)),
                    },
                },
                new Style(x => x.OfType<CheckBox>().Template().Id("checkMark"))
                {
                    Setters = new[]
                    {
                        new Setter(TextBlock.VisibilityProperty, Visibility.Hidden),
                    },
                },
                new Style(x => x.OfType<CheckBox>().Class(":checked").Template().Id("checkMark"))
                {
                    Setters = new[]
                    {
                        new Setter(TextBlock.VisibilityProperty, Visibility.Visible),
                    },
                },
            });
        }

        private Control Template(CheckBox control)
        {
            Border result = new Border
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Gap = 8,
                    Children = new PerspexList<Control>
                    {
                        new Border
                        {
                            BorderThickness = 2.0,
                            BorderBrush = new SolidColorBrush(0xff000000),
                            Padding = new Thickness(2),
                            Content = new TextBlock
                            {
                                Id = "checkMark",
                                Text = "Y",
                                Background = null,
                            },
                        },
                        new ContentPresenter
                        {
                        },
                    },
                },
            };

            result.TemplateBinding(control, Border.BackgroundProperty);
            StackPanel stack = (StackPanel)result.Content;
            ContentPresenter cp = (ContentPresenter)stack.Children[1];
            cp.TemplateBinding(control, ContentPresenter.ContentProperty);
            return result;
        }
    }
}

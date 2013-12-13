// -----------------------------------------------------------------------
// <copyright file="Button.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    public class Button : ContentControl
    {
        protected override Visual DefaultTemplate()
        {
            Border border = new Border();
            border.Background = new Perspex.Media.SolidColorBrush(0xff808080);
            border.BorderBrush = new Perspex.Media.SolidColorBrush(0xff000000);
            border.BorderThickness = 2;
            border.Padding = new Thickness(3);
            ContentPresenter contentPresenter = new ContentPresenter();
            contentPresenter.Bind(ContentPresenter.ContentProperty, this.GetObservable(ContentProperty));
            border.Content = contentPresenter;
            return border;
        }
    }
}

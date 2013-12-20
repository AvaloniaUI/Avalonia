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
            border.Bind(Border.BackgroundProperty, this.GetObservable(BackgroundProperty));
            border.Bind(Border.BorderBrushProperty, this.GetObservable(BorderBrushProperty));
            border.Bind(Border.BorderThicknessProperty, this.GetObservable(BorderThicknessProperty));
            border.Padding = new Thickness(3);
            ContentPresenter contentPresenter = new ContentPresenter();
            contentPresenter.Bind(ContentPresenter.ContentProperty, this.GetObservable(ContentProperty));
            border.Content = contentPresenter;
            return border;
        }
    }
}

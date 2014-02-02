// -----------------------------------------------------------------------
// <copyright file="Button.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;

    public class Button : ContentControl
    {
        public Button()
        {
            this.GetObservable(MouseLeftButtonDownEvent).Subscribe(e =>
            {
                this.Classes.Add(":pressed");
            });

            this.GetObservable(MouseLeftButtonUpEvent).Subscribe(e =>
            {
                this.Classes.Remove(":pressed");
            });
        }

        protected override Visual DefaultTemplate()
        {
            Border border = new Border();
            border.SetValue(Border.BackgroundProperty, this.GetObservable(Button.BackgroundProperty));
            border.SetValue(Border.BorderBrushProperty, this.GetObservable(Button.BorderBrushProperty));
            border.SetValue(Border.BorderThicknessProperty, this.GetObservable(Button.BorderThicknessProperty));
            border.Padding = new Thickness(3);
            ContentPresenter contentPresenter = new ContentPresenter();
            contentPresenter.SetValue(ContentPresenter.ContentProperty, this.GetObservable(Button.ContentProperty));
            border.Content = contentPresenter;
            return border;
        }
    }
}

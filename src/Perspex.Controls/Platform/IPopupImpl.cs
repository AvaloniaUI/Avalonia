





namespace Perspex.Platform
{
    using System;
    using Perspex.Controls;
    using Perspex.Input.Raw;

    public interface IPopupImpl : ITopLevelImpl
    {
        void SetPosition(Point p);

        void Show();

        void Hide();
    }
}

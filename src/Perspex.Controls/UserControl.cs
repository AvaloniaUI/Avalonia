





namespace Perspex.Controls
{
    using System;
    using Perspex.Styling;

    public class UserControl : ContentControl, IStyleable
    {
        Type IStyleable.StyleKey
        {
            get { return typeof(ContentControl); }
        }
    }
}

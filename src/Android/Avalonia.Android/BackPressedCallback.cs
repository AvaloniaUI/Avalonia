using System;
using System.Collections.Generic;
using System.Text;
using AndroidX.Activity;

namespace Avalonia.Android
{
    internal class BackPressedCallback(AvaloniaActivity activity) : OnBackPressedCallback(true)
    {
        public override void HandleOnBackPressed()
        {
            activity.OnBackInvoked();

            if (activity.ShouldNavigateBack)
            {
                this.Enabled = false;
                activity.OnBackPressedDispatcher?.OnBackPressed();
            }

            this.Enabled = true;
        }
    }
}

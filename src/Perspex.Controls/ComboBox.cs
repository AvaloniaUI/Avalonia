using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Controls.Primitives;
using Perspex.Layout;
using Perspex.Styling;

namespace Perspex.Controls
{
    /// <summary>
    /// Represents a selection control with a drop-down list that can be shown or hidden by clicking the arrow on the control.
    /// </summary>
    public class ComboBox : DropDown
    {
        public static readonly PerspexProperty<double> MaxDropDownHeightProperty =
            PerspexProperty.Register<ComboBox, double>("MaxDropDownHeight");

        public double MaxDropDownHeight
        {
            get { return GetValue(MaxDropDownHeightProperty); }
            set { SetValue(MaxDropDownHeightProperty, value); }
        }

        public static readonly PerspexProperty<bool> ShouldPreserveUserEnteredPrefixProperty =
            PerspexProperty.Register<ComboBox, bool>("ShouldPreserveUserEnteredPrefix");

        public bool ShouldPreserveUserEnteredPrefix
        {
            get { return GetValue(ShouldPreserveUserEnteredPrefixProperty); }
            set { SetValue(ShouldPreserveUserEnteredPrefixProperty, value); }
        }

        public static readonly PerspexProperty<bool> IsEditableProperty =
            PerspexProperty.Register<ComboBox, bool>("IsEditable");

        public bool IsEditable
        {
            get { return GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }

        public static readonly PerspexProperty<string> TextProperty =
            PerspexProperty.Register<ComboBox, string>("Text");

        public string Text
        {
            get { return GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        //TODO: Readonly Property
        public static readonly PerspexProperty<bool> IsReadOnlyProperty =
            PerspexProperty.Register<ComboBox, bool>("IsReadOnly");

        public bool IsReadOnly
        {
            get { return GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        private TextBox _editableTextBoxSite;
        private Popup _dropDownPopup;
    }
}

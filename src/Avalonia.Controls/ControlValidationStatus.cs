using Avalonia.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Controls
{
    public class ControlValidationStatus : ValidationStatus, INotifyPropertyChanged
    {
        private Dictionary<Type, ValidationStatus> propertyValidation = new Dictionary<Type, ValidationStatus>();

        public override bool IsValid => propertyValidation.Values.All(status => status.IsValid);

        public event PropertyChangedEventHandler PropertyChanged;

        public override bool Match(ValidationMethods enabledMethods) => true;

        public void UpdateValidationStatus(ValidationStatus status)
        {
            propertyValidation[status.GetType()] = status;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
        }
    }
}

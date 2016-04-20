using Perspex.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Controls
{
    public class ControlValidationStatus : IValidationStatus, INotifyPropertyChanged
    {
        private Dictionary<Type, IValidationStatus> propertyValidation = new Dictionary<Type, IValidationStatus>();

        public bool IsValid => propertyValidation.Values.All(status => status.IsValid);

        public event PropertyChangedEventHandler PropertyChanged;

        public void UpdateValidationStatus(IValidationStatus status)
        {
            propertyValidation[status.GetType()] = status;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
        }
    }
}

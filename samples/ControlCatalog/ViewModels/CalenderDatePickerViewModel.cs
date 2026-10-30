using System;
using System.ComponentModel.DataAnnotations;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    internal class CalenderDatePickerViewModel : ViewModelBase
    {
        /// <summary>
        /// A required DateTime which should demonstrate validation for the DateTimePicker
        /// </summary>
        [Required]
        public DateTime? ValidatedDateExample
        {
            get;
            set => RaiseAndSetIfChanged(ref field, value);
        }
    }
}

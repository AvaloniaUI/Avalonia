using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Media;

namespace ControlCatalog.Models
{
    public class Person : INotifyDataErrorInfo, INotifyPropertyChanged
    {
        string _firstName = string.Empty;
        string _lastName = string.Empty;
        bool _isBanned;
        private int _age;

        public string FirstName
        {
            get => _firstName;
            set
            {
                _firstName = value;
                if (string.IsNullOrWhiteSpace(value))
                    SetError(nameof(FirstName), "First Name Required");
                else
                    SetError(nameof(FirstName), null);

                OnPropertyChanged(nameof(FirstName));
            }

        }

        public string LastName
        {
            get => _lastName;
            set
            {
                _lastName = value;
                if (string.IsNullOrWhiteSpace(value))
                    SetError(nameof(LastName), "Last Name Required");
                else
                    SetError(nameof(LastName), null);

                OnPropertyChanged(nameof(LastName));
            }
        }

        public bool IsBanned
        {
            get => _isBanned;
            set
            {
                _isBanned = value;

                OnPropertyChanged(nameof(_isBanned));
            }
        }

        
        /// <summary>
        ///    Gets or sets the age of the person
        /// </summary>
        public int Age
        {
            get => _age;
            set
            {
                _age = value;
                OnPropertyChanged(nameof(Age));
            }
        }

        Dictionary<string, List<string>> _errorLookup = new Dictionary<string, List<string>>();

        void SetError(string propertyName, string? error)
        {
            if (string.IsNullOrEmpty(error))
            {
                if (_errorLookup.Remove(propertyName))
                    OnErrorsChanged(propertyName);
            }
            else
            {
                if (_errorLookup.TryGetValue(propertyName, out var errorList))
                {
                    errorList.Clear();
                    errorList.Add(error!);
                }
                else
                {
                    var errors = new List<string> { error! };
                    _errorLookup.Add(propertyName, errors);
                }

                OnErrorsChanged(propertyName);
            }
        }

        public bool HasErrors => _errorLookup.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
        void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public IEnumerable GetErrors(string? propertyName)
        {
            if (propertyName is { } && _errorLookup.TryGetValue(propertyName, out var errorList))
                return errorList;
            else
                return Array.Empty<object>();
        }
    }
}

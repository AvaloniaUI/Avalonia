using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Generators.Sandbox.ViewModels;

public class SignUpViewModel : ObservableValidator
{
    public SignUpViewModel()
    {
        UserName = "Joseph!";
        Password = "1234";
        ConfirmPassword = "1234";
        SignUp = new RelayCommand(() => { }, () => !HasErrors);

        ErrorsChanged += OnErrorsChanged;
    }

    public RelayCommand SignUp { get; }

    [Required]
    public string? UserName {
        get;
        set => SetProperty(ref field, value, validate: true);
    }

    public string? UserNameValidation
        => GetValidationMessage(nameof(UserName));

    [Required]
    [MinLength(2)]
    public string? Password
    {
        get;
        set
        {
            if (SetProperty(ref field, value, validate: true))
                ValidateProperty(ConfirmPassword, nameof(ConfirmPassword));
        }
    }

    public string? PasswordValidation
        => GetValidationMessage(nameof(Password));

    [Required]
    [Compare(nameof(Password))]
    public string? ConfirmPassword
    {
        get;
        set => SetProperty(ref field, value, validate: true);
    }

    public string? ConfirmPasswordValidation
        => GetValidationMessage(nameof(ConfirmPassword));

    public string? CompoundValidation
        => GetValidationMessage(null);

    private void OnErrorsChanged(object? sender, DataErrorsChangedEventArgs e)
    {
        if (e.PropertyName is not null)
            OnPropertyChanged(e.PropertyName + "Validation");

        OnPropertyChanged(CompoundValidation);
        SignUp.NotifyCanExecuteChanged();
    }

    private string? GetValidationMessage(string? propertyName)
    {
        var message = string.Join(Environment.NewLine, GetErrors(propertyName).Select(v => v.ErrorMessage));
        return string.IsNullOrEmpty(message) ? null : message;
    }
}

using System.Reactive;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace Generators.Sandbox.ViewModels;

public class SignUpViewModel : ReactiveValidationObject
{
    private string _userName = string.Empty;
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;

    public SignUpViewModel()
    {
        this.ValidationRule(
            vm => vm.UserName,
            name => !string.IsNullOrWhiteSpace(name),
            "UserName is required.");

        this.ValidationRule(
            vm => vm.Password,
            password => !string.IsNullOrWhiteSpace(password),
            "Password is required.");

        this.ValidationRule(
            vm => vm.Password,
            password => password?.Length > 2,
            password => $"Password should be longer, current length: {password.Length}");

        this.ValidationRule(
            vm => vm.ConfirmPassword,
            confirmation => !string.IsNullOrWhiteSpace(confirmation),
            "Confirm password field is required.");

        var passwordsObservable =
            this.WhenAnyValue(
                x => x.Password,
                x => x.ConfirmPassword,
                (password, confirmation) =>
                    password == confirmation);

        this.ValidationRule(
            vm => vm.ConfirmPassword,
            passwordsObservable,
            "Passwords must match.");

        SignUp = ReactiveCommand.Create(() => {}, this.IsValid());
    }

    public ReactiveCommand<Unit, Unit> SignUp { get; }

    public string UserName
    {
        get => _userName;
        set => this.RaiseAndSetIfChanged(ref _userName, value);
    }

    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set => this.RaiseAndSetIfChanged(ref _confirmPassword, value);
    }
}
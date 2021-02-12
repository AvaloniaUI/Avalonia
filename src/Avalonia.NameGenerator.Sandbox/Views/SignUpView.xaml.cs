using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace Avalonia.NameGenerator.Sandbox.Views
{
    /// <summary>
    /// This is a sample view class with typed x:Name references generated at compile-time using
    /// .NET 5 source generators. The class should be marked with [GenerateTypedNameReferences],
    /// this attribute is also compile-time generated. The class has to be partial because x:Name
    /// references are living in a separate partial class file. See also:
    /// https://devblogs.microsoft.com/dotnet/new-c-source-generator-samples/
    /// </summary>
    public partial class SignUpView : Window
    {
        public SignUpView()
        {
            AvaloniaXamlLoader.Load(this);
            
            UserNameTextBox.Text = "Joseph!";
            UserNameValidation.Text = "User name is valid.";
            PasswordTextBox.Text = "qwerty";
            PasswordValidation.Text = "Password is valid.";
            ConfirmPasswordTextBox.Text = "qwerty";
            ConfirmPasswordValidation.Text = "Password confirmation is valid.";
            SignUpButton.Content = "Sign up please!";
            CompoundValidation.Text = "Everything is okay.";
            AwesomeListView.VirtualizationMode = ItemVirtualizationMode.None;
        }
    }
}
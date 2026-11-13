using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Fluent2;
using Avalonia.Themes.Simple;
using Avalonia.Themes.UnitTests.Utilities;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Themes.UnitTests;

public class FluentDataValidationErrorsTests() : DataValidationErrorsTests(typeof(FluentTheme));
public class Fluent2DataValidationErrorsTests() : DataValidationErrorsTests(typeof(Fluent2Theme));
public class SimpleDataValidationErrorsTests() : DataValidationErrorsTests(typeof(SimpleTheme));

public abstract class DataValidationErrorsTests(Type typeEntryPoint) : ThemeTestBase(typeEntryPoint)
{
    [Theory]
    [InlineData(typeof(AutoCompleteBox))]
    [InlineData(typeof(CalendarDatePicker))]
    [InlineData(typeof(ComboBox))]
    [InlineData(typeof(DatePicker))]
    [InlineData(typeof(NumericUpDown))]
    [InlineData(typeof(Slider))]
    [InlineData(typeof(TextBox))]
    [InlineData(typeof(TimePicker))]
    public void Control_Template_Should_Display_Data_Validation_Errors(Type controlType)
    {
        CreateAttachedTheme();
        var control = (Control)Activator.CreateInstance(controlType)!;
        var error = new Exception("Error");
        DataValidationErrors.SetError(control, error);

        var window = new Window { Content = control };
        window.Show();
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        // Some templates forward the errors to an inner control (e.g. a TextBox) that displays them.
        Assert.Contains(control.GetVisualDescendants().OfType<DataValidationErrors>(),
            x => x.Owner is { } owner && DataValidationErrors.GetErrors(owner)?.Contains(error) == true);
        window.Close();
    }
}

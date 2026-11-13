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

public class FluentComboBoxTests() : ComboBoxTests(typeof(FluentTheme));
public class Fluent2ComboBoxTests() : ComboBoxTests(typeof(Fluent2Theme));
public class SimpleComboBoxTests() : ComboBoxTests(typeof(SimpleTheme));

public abstract class ComboBoxTests(Type typeEntryPoint) : ThemeTestBase(typeEntryPoint)
{
    [Fact]
    public void ComboBox_Template_Should_Contain_DataValidationErrors_Owned_By_ComboBox()
    {
        CreateAttachedTheme();
        var comboBox = new ComboBox();
        DataValidationErrors.SetError(comboBox, new Exception("Error"));

        var window = new Window { Content = comboBox };
        window.Show();
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        var errors = comboBox.GetVisualDescendants().OfType<DataValidationErrors>().SingleOrDefault();
        Assert.NotNull(errors);
        Assert.Same(comboBox, errors.Owner);
        window.Close();
    }
}

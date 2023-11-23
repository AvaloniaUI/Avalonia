using System;
using Avalonia.Controls;

namespace Generators.Sandbox.Controls;

public class CustomTextBox : TextBox
{
    protected override Type StyleKeyOverride => typeof(TextBox);
}

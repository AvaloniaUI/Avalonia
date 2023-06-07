using System;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Generators.Sandbox.Controls;

public class CustomTextBox : TextBox, IStyleable
{
    Type IStyleable.StyleKey => typeof(TextBox);
}